using System.Text.Json;
using OrderService.Common.Exceptions;
using OrderService.DTOs;
using OrderService.Messaging;
using OrderService.Models;
using OrderService.Repositories;

namespace OrderService.Services
{
    public class OrderService : IOrderService
    {
        private const string PendingStatus = "PENDING";
        private const string ProcessingStatus = "PROCESSING";
        private const string ShippedStatus = "SHIPPED";
        private const string DeliveredStatus = "DELIVERED";
        private const string CancelledStatus = "CANCELLED";

        private readonly IOrderRepository _orderRepository;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IProductCatalogClient _productCatalogClient;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            IKafkaProducer kafkaProducer,
            IProductCatalogClient productCatalogClient,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _kafkaProducer = kafkaProducer;
            _productCatalogClient = productCatalogClient;
            _logger = logger;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto)
        {
            if (!Guid.TryParse(dto.CustomerId, out var customerId))
            {
                throw new BadRequestException("CustomerId must be a valid GUID.");
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                TotalAmount = dto.TotalAmount,
                Status = PendingStatus,
                Currency = "USD",
                Notes = BuildStructuredNotes(dto.ExternalOrderId, null, null),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _orderRepository.AddAsync(order);

            _logger.LogInformation("Order created successfully. OrderId: {OrderId}", order.Id);
            await PublishOrderEventAsync("order.created", order);

            return MapToResponse(order);
        }

        public async Task<EcommerceOrderResponseDto> CreateEcommerceOrderAsync(EcommerceCreateOrderDto dto)
        {
            if (dto.CustomerId == Guid.Empty)
            {
                throw new BadRequestException("CustomerId is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.ExternalOrderId) ||
                string.IsNullOrWhiteSpace(dto.PaymentMethod) ||
                dto.ShippingAddressSnapshot == null ||
                dto.Items.Count == 0)
            {
                throw new BadRequestException("ExternalOrderId, paymentMethod, shippingAddressSnapshot, and items are required.");
            }

            var normalizedItems = dto.Items
                .Where(item => item.ProductId != Guid.Empty && item.Quantity > 0)
                .GroupBy(item => item.ProductId)
                .Select(group => new EcommerceOrderItemRequestDto
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(item => item.Quantity)
                })
                .ToList();

            if (normalizedItems.Count == 0)
            {
                throw new BadRequestException("At least one valid order item is required.");
            }

            var resolvedProducts = await _productCatalogClient.ResolveProductsAsync(normalizedItems.Select(item => item.ProductId));
            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0m;

            foreach (var item in normalizedItems)
            {
                if (!resolvedProducts.TryGetValue(item.ProductId, out var product))
                {
                    throw new NotFoundException($"Product {item.ProductId} was not found.");
                }

                if (!product.IsActive)
                {
                    throw new BadRequestException($"Product {product.Name} is inactive.");
                }

                if (product.QuantityAvailable < item.Quantity)
                {
                    throw new ConflictException($"Insufficient stock for product {product.Name}. Available: {product.QuantityAvailable}, Requested: {item.Quantity}.");
                }

                var totalPrice = product.Price * item.Quantity;
                totalAmount += totalPrice;

                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = totalPrice,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var metadata = new EcommerceMetadata
            {
                PaymentMethod = dto.PaymentMethod.Trim(),
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                ShippingAddress = dto.ShippingAddressSnapshot
            };

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = dto.CustomerId,
                TotalAmount = totalAmount,
                Status = PendingStatus,
                Currency = "USD",
                Notes = BuildStructuredNotes(dto.ExternalOrderId.Trim(), null, metadata),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var item in orderItems)
            {
                item.OrderId = order.Id;
            }

            await _orderRepository.AddWithItemsAsync(order, orderItems);

            var deductedItems = new List<OrderItem>();
            try
            {
                foreach (var item in orderItems)
                {
                    await _productCatalogClient.DeductStockAsync(order.Id, item.ProductId, item.Quantity);
                    deductedItems.Add(item);
                }
            }
            catch
            {
                await CompensateFailedCheckoutAsync(order, deductedItems);
                throw;
            }

            _logger.LogInformation("Ecommerce order created successfully. OrderId: {OrderId}", order.Id);
            await PublishOrderEventAsync("order.created", order);

            return MapToEcommerceResponse(order);
        }

        public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllAsync();
            return orders.Select(MapToResponse).ToList();
        }

        public async Task<OrderResponseDto> GetOrderByIdAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                throw new NotFoundException("Order not found.");
            }

            return MapToResponse(order);
        }

        public async Task<IReadOnlyList<EcommerceOrderResponseDto>> GetOrdersByCustomerAsync(Guid customerId)
        {
            var orders = await _orderRepository.GetByCustomerIdAsync(customerId);
            return orders.Select(MapToEcommerceResponse).ToList();
        }

        public async Task<EcommerceOrderResponseDto> GetEcommerceOrderByIdAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdWithItemsAsync(id);
            if (order == null)
            {
                throw new NotFoundException("Order not found.");
            }

            return MapToEcommerceResponse(order);
        }

        public async Task<EcommerceOrderResponseDto> CancelEcommerceOrderAsync(Guid id, string? reason)
        {
            var order = await _orderRepository.GetByIdWithItemsAsync(id);
            if (order == null)
            {
                throw new NotFoundException("Order not found.");
            }

            var cancellationReason = string.IsNullOrWhiteSpace(reason) ? "Cancelled by customer." : reason.Trim();
            ValidateStatusTransition(order, CancelledStatus, cancellationReason);

            var releasedItems = new List<OrderItem>();
            try
            {
                foreach (var item in order.Items)
                {
                    await _productCatalogClient.ReleaseStockAsync(order.Id, item.ProductId, item.Quantity);
                    releasedItems.Add(item);
                }
            }
            catch
            {
                await ReapplyReleasedStockAsync(order.Id, releasedItems);
                throw;
            }

            var metadata = ExtractEcommerceMetadata(order.Notes);
            var externalOrderId = ExtractExternalOrderId(order.Notes);

            order.Status = CancelledStatus;
            order.UpdatedAt = DateTime.UtcNow;
            order.Notes = BuildStructuredNotes(externalOrderId, cancellationReason, metadata);

            await _orderRepository.UpdateAsync(order);
            await PublishOrderEventAsync("order.status.changed", order);

            return MapToEcommerceResponse(order);
        }

        public async Task<OrderResponseDto> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto dto)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                throw new NotFoundException("Order not found.");
            }

            var targetStatus = NormalizeStatus(dto.RequestedStatus);
            ValidateStatusTransition(order, targetStatus, dto.CancellationReason);

            var externalOrderId = ExtractExternalOrderId(order.Notes);
            var metadata = ExtractEcommerceMetadata(order.Notes);

            order.Status = targetStatus;
            order.UpdatedAt = DateTime.UtcNow;

            if (targetStatus == CancelledStatus)
            {
                order.Notes = BuildStructuredNotes(externalOrderId, dto.CancellationReason, metadata) ?? dto.CancellationReason;
            }

            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("Order status updated. OrderId: {OrderId}, NewStatus: {NewStatus}",
                order.Id, order.Status);

            await PublishOrderEventAsync("order.status.changed", order);

            return MapToResponse(order);
        }

        public async Task<object> GetOrderReportAsync()
        {
            var orders = await _orderRepository.GetAllAsync();

            var normalizedStatuses = orders.Select(o => NormalizeStatus(o.Status)).ToList();

            var report = new
            {
                TotalOrders = orders.Count,
                Pending = normalizedStatuses.Count(s => s == PendingStatus),
                Processing = normalizedStatuses.Count(s => s == ProcessingStatus),
                Shipped = normalizedStatuses.Count(s => s == ShippedStatus),
                Delivered = normalizedStatuses.Count(s => s == DeliveredStatus),
                Cancelled = normalizedStatuses.Count(s => s == CancelledStatus),
                TotalRevenueFromDelivered = orders
                    .Where(o => NormalizeStatus(o.Status) == DeliveredStatus)
                    .Sum(o => o.TotalAmount)
            };

            return report;
        }

        private void ValidateStatusTransition(Order order, string targetStatus, string? cancellationReason)
        {
            var currentStatus = NormalizeStatus(order.Status);

            if (string.IsNullOrWhiteSpace(targetStatus))
            {
                throw new BadRequestException("A target status is required.");
            }

            if (currentStatus == CancelledStatus)
            {
                throw new BadRequestException("Cancelled orders cannot be updated.");
            }

            if (currentStatus == DeliveredStatus)
            {
                throw new BadRequestException("Delivered orders cannot be updated.");
            }

            if (targetStatus == CancelledStatus)
            {
                if (string.IsNullOrWhiteSpace(cancellationReason))
                {
                    throw new BadRequestException("Cancellation reason is required when cancelling an order.");
                }

                return;
            }

            var isValid = currentStatus switch
            {
                PendingStatus => targetStatus == ProcessingStatus,
                ProcessingStatus => targetStatus == ShippedStatus,
                ShippedStatus => targetStatus == DeliveredStatus,
                _ => false
            };

            if (!isValid)
            {
                throw new BadRequestException(
                    $"Invalid status transition from {currentStatus} to {targetStatus}.");
            }
        }

        private async Task CompensateFailedCheckoutAsync(Order order, IEnumerable<OrderItem> deductedItems)
        {
            foreach (var item in deductedItems.Reverse())
            {
                try
                {
                    await _productCatalogClient.ReleaseStockAsync(order.Id, item.ProductId, item.Quantity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to release stock during checkout compensation. OrderId: {OrderId}, ProductId: {ProductId}",
                        order.Id, item.ProductId);
                }
            }

            try
            {
                await _orderRepository.DeleteAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete ecommerce order during checkout compensation. OrderId: {OrderId}", order.Id);
            }
        }

        private async Task ReapplyReleasedStockAsync(Guid orderId, IEnumerable<OrderItem> releasedItems)
        {
            foreach (var item in releasedItems.Reverse())
            {
                try
                {
                    await _productCatalogClient.DeductStockAsync(orderId, item.ProductId, item.Quantity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to re-deduct stock after cancellation compensation. OrderId: {OrderId}, ProductId: {ProductId}",
                        orderId, item.ProductId);
                }
            }
        }

        private OrderResponseDto MapToResponse(Order order)
        {
            var status = NormalizeStatus(order.Status);
            var externalOrderId = ExtractExternalOrderId(order.Notes);
            var cancellationReason = status == CancelledStatus
                ? ExtractCancellationReason(order.Notes) ?? order.Notes
                : null;

            return new OrderResponseDto
            {
                Id = order.Id,
                ExternalOrderId = externalOrderId ?? string.Empty,
                CustomerId = order.CustomerId.ToString(),
                TotalAmount = order.TotalAmount,
                Status = status,
                CancellationReason = cancellationReason,
                CreatedAt = order.CreatedAt,
                ConfirmedAt = null,
                ProcessedAt = null,
                ShippedAt = null,
                DeliveredAt = null,
                CancelledAt = status == CancelledStatus ? order.UpdatedAt : null
            };
        }

        private EcommerceOrderResponseDto MapToEcommerceResponse(Order order)
        {
            var metadata = ExtractEcommerceMetadata(order.Notes) ?? new EcommerceMetadata();

            return new EcommerceOrderResponseDto
            {
                Id = order.Id,
                ExternalOrderId = ExtractExternalOrderId(order.Notes) ?? string.Empty,
                CustomerId = order.CustomerId,
                TotalAmount = order.TotalAmount,
                Status = NormalizeStatus(order.Status),
                Currency = order.Currency,
                PaymentMethod = metadata.PaymentMethod,
                Notes = metadata.Notes,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                ShippingAddress = metadata.ShippingAddress ?? new ShippingAddressSnapshotDto(),
                Items = order.Items
                    .OrderBy(item => item.ProductName)
                    .Select(item => new EcommerceOrderItemResponseDto
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    })
                    .ToList()
            };
        }

        private async Task PublishOrderEventAsync(string topic, Order order)
        {
            var payload = JsonSerializer.Serialize(new
            {
                order.Id,
                CustomerId = order.CustomerId,
                Status = NormalizeStatus(order.Status),
                order.TotalAmount,
                order.CreatedAt
            });

            await _kafkaProducer.PublishAsync(topic, order.Id.ToString(), payload);
        }

        private static string NormalizeStatus(string? status)
        {
            return status?.Trim().ToUpperInvariant() switch
            {
                "CREATED" => PendingStatus,
                "CONFIRMED" => ProcessingStatus,
                PendingStatus => PendingStatus,
                ProcessingStatus => ProcessingStatus,
                ShippedStatus => ShippedStatus,
                DeliveredStatus => DeliveredStatus,
                CancelledStatus => CancelledStatus,
                _ => throw new BadRequestException($"Unsupported order status '{status}'.")
            };
        }

        private static string? BuildStructuredNotes(string? externalOrderId, string? cancellationReason, EcommerceMetadata? metadata)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(externalOrderId))
            {
                parts.Add($"ExternalOrderId:{externalOrderId}");
            }

            if (!string.IsNullOrWhiteSpace(cancellationReason))
            {
                parts.Add($"CancellationReason:{cancellationReason}");
            }

            if (metadata != null)
            {
                parts.Add($"EcommerceMetadata:{JsonSerializer.Serialize(metadata)}");
            }

            return parts.Count == 0 ? null : string.Join(Environment.NewLine, parts);
        }

        private static string? ExtractExternalOrderId(string? notes)
            => ExtractStructuredValue(notes, "ExternalOrderId");

        private static string? ExtractCancellationReason(string? notes)
            => ExtractStructuredValue(notes, "CancellationReason");

        private static EcommerceMetadata? ExtractEcommerceMetadata(string? notes)
        {
            var metadataValue = ExtractStructuredValue(notes, "EcommerceMetadata");
            return string.IsNullOrWhiteSpace(metadataValue)
                ? null
                : JsonSerializer.Deserialize<EcommerceMetadata>(metadataValue);
        }

        private static string? ExtractStructuredValue(string? notes, string key)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                return null;
            }

            foreach (var line in notes.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var prefix = $"{key}:";
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return line[prefix.Length..].Trim();
                }
            }

            return null;
        }

        private sealed class EcommerceMetadata
        {
            public string PaymentMethod { get; set; } = string.Empty;
            public string? Notes { get; set; }
            public ShippingAddressSnapshotDto? ShippingAddress { get; set; }
        }
    }
}

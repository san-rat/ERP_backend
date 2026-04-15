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
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            IKafkaProducer kafkaProducer,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _kafkaProducer = kafkaProducer;
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
                Notes = BuildStructuredNotes(dto.ExternalOrderId, null),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _orderRepository.AddAsync(order);

            _logger.LogInformation("Order created successfully. OrderId: {OrderId}", order.Id);

            await PublishOrderEventAsync("order.created", order);

            return MapToResponse(order);
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

            order.Status = targetStatus;
            order.UpdatedAt = DateTime.UtcNow;

            if (targetStatus == CancelledStatus)
            {
                order.Notes = BuildStructuredNotes(externalOrderId, dto.CancellationReason) ?? dto.CancellationReason;
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

        private static string? BuildStructuredNotes(string? externalOrderId, string? cancellationReason)
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

            return parts.Count == 0 ? null : string.Join(Environment.NewLine, parts);
        }

        private static string? ExtractExternalOrderId(string? notes)
            => ExtractStructuredValue(notes, "ExternalOrderId");

        private static string? ExtractCancellationReason(string? notes)
            => ExtractStructuredValue(notes, "CancellationReason");

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
    }
}

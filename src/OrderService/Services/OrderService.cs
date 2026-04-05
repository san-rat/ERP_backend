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
            // Avoid duplicate external order ids
            var existing = await _orderRepository.GetByExternalOrderIdAsync(dto.ExternalOrderId);
            if (existing != null)
            {
                throw new BadRequestException("An order with this ExternalOrderId already exists.");
            }

            var order = new Order
            {
                ExternalOrderId = dto.ExternalOrderId,
                CustomerId = dto.CustomerId,
                TotalAmount = dto.TotalAmount,
                Status = OrderStatus.Created,
                CreatedAt = DateTime.UtcNow
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

        public async Task<OrderResponseDto> GetOrderByIdAsync(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                throw new NotFoundException("Order not found.");
            }

            return MapToResponse(order);
        }

        public async Task<OrderResponseDto> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                throw new NotFoundException("Order not found.");
            }

            ValidateStatusTransition(order, dto);

            switch (dto.NewStatus)
            {
                case OrderStatus.Confirmed:
                    order.Status = OrderStatus.Confirmed;
                    order.ConfirmedAt = DateTime.UtcNow;
                    break;

                case OrderStatus.Processing:
                    order.Status = OrderStatus.Processing;
                    order.ProcessedAt = DateTime.UtcNow;
                    break;

                case OrderStatus.Shipped:
                    order.Status = OrderStatus.Shipped;
                    order.ShippedAt = DateTime.UtcNow;
                    break;

                case OrderStatus.Delivered:
                    order.Status = OrderStatus.Delivered;
                    order.DeliveredAt = DateTime.UtcNow;
                    break;

                case OrderStatus.Cancelled:
                    order.Status = OrderStatus.Cancelled;
                    order.CancellationReason = dto.CancellationReason;
                    order.CancelledAt = DateTime.UtcNow;
                    break;

                default:
                    throw new BadRequestException("Unsupported status update.");
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

            var report = new
            {
                TotalOrders = orders.Count,
                Created = orders.Count(o => o.Status == OrderStatus.Created),
                Confirmed = orders.Count(o => o.Status == OrderStatus.Confirmed),
                Processing = orders.Count(o => o.Status == OrderStatus.Processing),
                Shipped = orders.Count(o => o.Status == OrderStatus.Shipped),
                Delivered = orders.Count(o => o.Status == OrderStatus.Delivered),
                Cancelled = orders.Count(o => o.Status == OrderStatus.Cancelled),
                TotalRevenueFromDelivered = orders
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .Sum(o => o.TotalAmount)
            };

            return report;
        }

        private void ValidateStatusTransition(Order order, UpdateOrderStatusDto dto)
        {
            // Once cancelled or delivered, no more changes
            if (order.Status == OrderStatus.Cancelled)
            {
                throw new BadRequestException("Cancelled orders cannot be updated.");
            }

            if (order.Status == OrderStatus.Delivered)
            {
                throw new BadRequestException("Delivered orders cannot be updated.");
            }

            // Cancel rules
            if (dto.NewStatus == OrderStatus.Cancelled)
            {
                if (string.IsNullOrWhiteSpace(dto.CancellationReason))
                {
                    throw new BadRequestException("Cancellation reason is required when cancelling an order.");
                }

                // Allowed anytime before delivered
                return;
            }

            // Only valid forward flow is allowed
            bool isValid = order.Status switch
            {
                OrderStatus.Created => dto.NewStatus == OrderStatus.Confirmed,
                OrderStatus.Confirmed => dto.NewStatus == OrderStatus.Processing,
                OrderStatus.Processing => dto.NewStatus == OrderStatus.Shipped,
                OrderStatus.Shipped => dto.NewStatus == OrderStatus.Delivered,
                _ => false
            };

            if (!isValid)
            {
                throw new BadRequestException(
                    $"Invalid status transition from {order.Status} to {dto.NewStatus}.");
            }
        }

        private OrderResponseDto MapToResponse(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                ExternalOrderId = order.ExternalOrderId,
                CustomerId = order.CustomerId,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                CancellationReason = order.CancellationReason,
                CreatedAt = order.CreatedAt,
                ConfirmedAt = order.ConfirmedAt,
                ProcessedAt = order.ProcessedAt,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                CancelledAt = order.CancelledAt
            };
        }

        private async Task PublishOrderEventAsync(string topic, Order order)
        {
            var payload = JsonSerializer.Serialize(new
            {
                order.Id,
                order.ExternalOrderId,
                Status = order.Status.ToString(),
                order.CustomerId,
                order.TotalAmount,
                order.CreatedAt
            });

            await _kafkaProducer.PublishAsync(topic, order.Id.ToString(), payload);
        }
    }
}
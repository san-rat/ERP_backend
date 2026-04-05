using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Common.Responses;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controller
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        // Create Order
        // In a real microservice setup, this might be called by ApiGateway or e-commerce app
        // For demo/testing, we allow authenticated Employee role
        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var result = await _orderService.CreateOrderAsync(dto);
            return CreatedAtAction(nameof(GetOrderById),
                new { id = result.Id },
                ApiResponse<OrderResponseDto>.Ok("Order created successfully.", result));
        }

        // Admin -> read only
        // Manager -> can view too
        // Employee -> can also view if needed during processing
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> GetAllOrders()
        {
            var result = await _orderService.GetAllOrdersAsync();
            return Ok(ApiResponse<List<OrderResponseDto>>.Ok("Orders fetched successfully.", result));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var result = await _orderService.GetOrderByIdAsync(id);
            return Ok(ApiResponse<OrderResponseDto>.Ok("Order fetched successfully.", result));
        }

        // Employee only can change order status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            var result = await _orderService.UpdateOrderStatusAsync(id, dto);
            return Ok(ApiResponse<OrderResponseDto>.Ok("Order status updated successfully.", result));
        }

        // Manager only report endpoint
        [HttpGet("reports/summary")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetOrderReport()
        {
            var result = await _orderService.GetOrderReportAsync();
            return Ok(ApiResponse<object>.Ok("Order report fetched successfully.", result));
        }
    }
}
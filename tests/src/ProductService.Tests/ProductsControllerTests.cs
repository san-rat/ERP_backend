using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductService.Controllers;
using ProductService.DTOs;
using ProductService.Common;
using ProductService.Interfaces;
using Xunit;

namespace ProductService.Tests
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductManager> _managerMock;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _managerMock = new Mock<IProductManager>();
            _controller = new ProductsController(_managerMock.Object);
        }

        private void SetupUserClaims(Guid userId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task CreateProduct_ValidRequest_ReturnsCreated()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId);

            var dto = new CreateProductDto { Sku = "CONTROLLER-TEST", Price = 99 };
            var responseDto = new ProductResponseDto { Id = Guid.NewGuid(), Sku = "CONTROLLER-TEST" };

            _managerMock.Setup(m => m.CreateProductAsync(userId, dto))
                .ReturnsAsync(responseDto);

            // Act
            var result = await _controller.CreateProduct(dto);

            // Assert
            var actionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal("GetProductById", actionResult.ActionName);
            var returnedValue = Assert.IsType<ProductResponseDto>(actionResult.Value);
            Assert.Equal(responseDto.Id, returnedValue.Id);
        }

        [Fact]
        public async Task GetProducts_CallsManagerCorrectly_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId);

            var term = "laptop";
            var mockResponse = new PaginatedResponse<ProductResponseDto>();

            _managerMock.Setup(m => m.GetProductsAsync(userId, 1, 10, null, term))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _controller.GetProducts(1, 10, null, term);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(mockResponse, okResult.Value);
        }

        [Fact]
        public async Task DeductStock_InsufficientStock_ReturnsConflict()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId);

            var dto = new DeductStockDto { ProductId = Guid.NewGuid(), OrderId = Guid.NewGuid(), Quantity = 5 };
            
            _managerMock.Setup(m => m.DeductStockAsync(userId, dto))
                .ReturnsAsync((false, "Insufficient stock."));

            // Act
            var result = await _controller.DeductStock(dto);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.NotNull(conflictResult.Value);
        }
        
        [Fact]
        public async Task DeductStock_SufficientStock_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId);

            var dto = new DeductStockDto { ProductId = Guid.NewGuid(), OrderId = Guid.NewGuid(), Quantity = 5 };
            
            _managerMock.Setup(m => m.DeductStockAsync(userId, dto))
                .ReturnsAsync((true, "Successful"));

            // Act
            var result = await _controller.DeductStock(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
        }
    }
}

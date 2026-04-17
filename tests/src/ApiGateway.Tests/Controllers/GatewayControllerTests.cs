using ApiGateway.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ApiGateway.Tests.Controllers
{
    public class GatewayControllerTests
    {
        private readonly Mock<ILogger<GatewayController>> _mockLogger;
        private readonly GatewayController _controller;

        public GatewayControllerTests()
        {
            _mockLogger = new Mock<ILogger<GatewayController>>();
            _controller = new GatewayController(_mockLogger.Object);
        }

        [Fact]
        public void Health_ReturnsOkResult_WithHealthyStatus()
        {
            // Act
            var result = _controller.Health();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var status = GetProperty(okResult.Value, "status");
            Assert.Equal("healthy", status);
        }

        [Fact]
        public void GetInfo_ReturnsOkResult_WithGatewayDetails()
        {
            // Act
            var result = _controller.GetInfo();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var service = GetProperty(okResult.Value, "service");
            Assert.Equal("ApiGateway", service);
            
            var framework = GetProperty(okResult.Value, "framework");
            Assert.Equal(".NET 9.0", framework);

            var endpoints = GetProperty(okResult.Value, "endpoints");
            var adminEndpoint = GetProperty(endpoints, "admin");
            Assert.Equal("/api/admin/*", adminEndpoint);
        }

        [Fact]
        public void GetServices_ReturnsOkResult_WithServicesList()
        {
            // Act
            var result = _controller.GetServices();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var totalServices = GetProperty(okResult.Value, "totalServices");
            Assert.True(totalServices is int count && count >= 8);

            var services = Assert.IsAssignableFrom<System.Collections.IEnumerable>(GetProperty(okResult.Value, "services"));
            Assert.Contains(services.Cast<object>(), service => Equals(GetProperty(service, "name"), "AdminService"));
        }

        [Fact]
        public void GetRoutes_ReturnsOkResult_WithRoutesList()
        {
            // Act
            var result = _controller.GetRoutes();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var routes = Assert.IsAssignableFrom<System.Collections.IEnumerable>(GetProperty(okResult.Value, "routes"));
            Assert.Contains(routes.Cast<object>(), route => Equals(GetProperty(route, "upstream"), "/api/admin/*"));
            Assert.Contains(routes.Cast<object>(), route => Equals(GetProperty(route, "upstream"), "/admin/health"));
            Assert.Contains(routes.Cast<object>(), route => Equals(GetProperty(route, "upstream"), "/auth/db-check"));
            Assert.Contains(routes.Cast<object>(), route => Equals(GetProperty(route, "upstream"), "/prediction/db-check"));
            Assert.Contains(routes.Cast<object>(), route => Equals(GetProperty(route, "upstream"), "/forecast/swagger/*"));
        }

        [Fact]
        public void IsAlive_ReturnsOkResult_WithAliveStatus()
        {
            // Act
            var result = _controller.IsAlive();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var status = GetProperty(okResult.Value, "status");
            Assert.Equal("alive", status);
        }

        [Fact]
        public void IsReady_ReturnsOkResult_WithReadyStatus()
        {
            // Act
            var result = _controller.IsReady();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var status = GetProperty(okResult.Value, "status");
            Assert.Equal("ready", status);
        }

        // Helper to access properties of anonymous types via reflection
        private static object? GetProperty(object? obj, string propertyName)
        {
            if (obj == null) return null;
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj, null);
        }
    }
}

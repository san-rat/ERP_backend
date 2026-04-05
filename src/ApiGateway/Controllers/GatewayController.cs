using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace ApiGateway.Controllers
{
    /// <summary>
    /// API Gateway Health and Information Endpoints
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class GatewayController : ControllerBase
    {
        private readonly ILogger<GatewayController> _logger;

        public GatewayController(ILogger<GatewayController> logger)
        {
            _logger = logger;
        }

        /// <summary>Returns 200 OK when gateway is healthy. No JWT required.</summary>
        [AllowAnonymous]
        [HttpGet("health")]
        [HttpHead("health")]
        public IActionResult Health()
        {
            _logger.LogInformation("Health check requested");
            return Ok(new
            {
                service     = Assembly.GetExecutingAssembly().GetName().Name,
                status      = "healthy",
                version     = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                timestamp   = DateTime.UtcNow,
                uptime      = GetUptime()
            });
        }

        /// <summary>Returns gateway info, features and registered routes. No JWT required.</summary>
        [AllowAnonymous]
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            _logger.LogInformation("Gateway info requested");
            return Ok(new
            {
                service     = "ApiGateway",
                description = "Ocelot API Gateway with static service registry",
                version     = "1.0.0",
                framework   = ".NET 9.0",
                apiGateway  = "Ocelot 24.1.0",
                features    = new[]
                {
                    "Static Service Registry",
                    "Rate Limiting",
                    "JWT Authentication",
                    "CORS Support",
                    "Circuit Breaker (QoS)",
                    "Request Logging",
                    "Health Checks"
                },
                endpoints = new
                {
                    authentication = "/api/auth/*",
                    customers      = "/api/customers/*",
                    orders         = "/api/orders/*",
                    products       = "/api/products/*",
                    admin          = "/api/admin/*",
                    ml_forecast    = "/api/ml/forecast/*",
                    ml_churn       = "/api/ml/churn/*",
                    ml_segmentation= "/api/ml/segmentation/*",
                    dashboard      = "/api/dashboard/*",
                    reports        = "/api/reports/*",
                    chatbot        = "/api/chatbot/*",
                    notifications  = "/api/notifications/*"
                },
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>Returns all statically registered downstream services. No JWT required.</summary>
        [AllowAnonymous]
        [HttpGet("services")]
        public IActionResult GetServices()
        {
            _logger.LogInformation("Services info requested");
            return Ok(new
            {
                totalServices = 11,
                services = new[]
                {
                    new { microservice = 1, name = "AuthService",            host = "AuthService",            port = 5001, description = "Account Management — registration, login, JWT, RBAC, MFA, API keys"  },
                    new { microservice = 2, name = "CustomerService",        host = "CustomerService",        port = 5002, description = "Customers — CRUD, search, segments, attributes"                     },
                    new { microservice = 3, name = "OrderService",           host = "OrderService",           port = 5003, description = "Orders — create, status, cancellation, returns & refunds"           },
                    new { microservice = 4, name = "ProductService",         host = "ProductService",         port = 5004, description = "Products — CRUD, inventory reserve/release, low-stock alerts"       },
                    new { microservice = 5, name = "ForecastService",        host = "ForecastService",        port = 5005, description = "Forecast Service — demand & revenue forecasting"                    },
                    new { microservice = 6, name = "PredictionService",      host = "PredictionService",      port = 5006, description = "Prediction Service — churn prediction & customer segmentation"      },
                    new { microservice = 7, name = "AnalyticsService",       host = "AnalyticsService",       port = 5007, description = "Analytics Service — dashboards & KPI metrics"                      },
                    new { microservice = 8, name = "AdminService",           host = "AdminService",           port = 5011, description = "Admin Service — staff management, password resets, dashboard overview" },
                    new { microservice = 9, name = "report-service",         host = "report-service",         port = 5008, description = "Report Service — PDF/Excel report generation"                       },
                    new { microservice = 10,name = "chatbot-service",        host = "chatbot-service",        port = 5009, description = "Chatbot Service — AI assistant"                                     },
                    new { microservice = 11,name = "notification-service",   host = "notification-service",   port = 5010, description = "Notification Service — push, email, SMS"                            }
                },
                note      = "Hosts resolve via Docker network by container name",
                swaggerUI = "http://localhost:5000/swagger",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>Returns gateway route table. No JWT required.</summary>
        [AllowAnonymous]
        [HttpGet("routes")]
        public IActionResult GetRoutes()
        {
            _logger.LogInformation("Routes info requested");
            return Ok(new
            {
                totalRoutes = 23,
                routes = new object[]
                {
                    new { upstream = "/api/auth/*",             downstream = "AuthService:5001",            auth = false },
                    new { upstream = "/api/customers/*",        downstream = "CustomerService:5002",        auth = true  },
                    new { upstream = "/api/orders/*",           downstream = "OrderService:5003",           auth = true  },
                    new { upstream = "/api/products/*",         downstream = "ProductService:5004",         auth = true  },
                    new { upstream = "/api/admin/*",            downstream = "AdminService:5011",           auth = true  },
                    new { upstream = "/api/ml/forecast/*",      downstream = "ForecastService:5005",        auth = true  },
                    new { upstream = "/api/ml/churn/*",         downstream = "PredictionService:5006",      auth = true  },
                    new { upstream = "/api/ml/segmentation/*",  downstream = "PredictionService:5006",      auth = true  },
                    new { upstream = "/api/dashboard/*",        downstream = "AnalyticsService:5007",       auth = true  },
                    new { upstream = "/api/reports/*",          downstream = "report-service:5008",         auth = true  },
                    new { upstream = "/api/chatbot/*",          downstream = "chatbot-service:5009",        auth = true  },
                    new { upstream = "/api/notifications/*",    downstream = "notification-service:5010",   auth = true  },
                    // Per-service health probes (no JWT)
                    new { upstream = "/auth/health",            downstream = "AuthService:5001",            auth = false },
                    new { upstream = "/customer/health",        downstream = "CustomerService:5002",        auth = false },
                    new { upstream = "/order/health",           downstream = "OrderService:5003",           auth = false },
                    new { upstream = "/product/health",         downstream = "ProductService:5004",         auth = false },
                    new { upstream = "/admin/health",           downstream = "AdminService:5011",           auth = false },
                    new { upstream = "/forecast/health",        downstream = "ForecastService:5005",        auth = false },
                    new { upstream = "/prediction/health",      downstream = "PredictionService:5006",      auth = false },
                    new { upstream = "/analytics/health",       downstream = "AnalyticsService:5007",       auth = false },
                    new { upstream = "/report/health",          downstream = "report-service:5008",         auth = false },
                    new { upstream = "/chatbot/health",         downstream = "chatbot-service:5009",        auth = false },
                    new { upstream = "/notification/health",    downstream = "notification-service:5010",   auth = false }
                },
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>Kubernetes liveness probe. No JWT required.</summary>
        [AllowAnonymous]
        [HttpGet("live")]
        [HttpHead("live")]
        public IActionResult IsAlive() =>
            Ok(new { status = "alive", timestamp = DateTime.UtcNow });

        /// <summary>Kubernetes readiness probe. No JWT required.</summary>
        [AllowAnonymous]
        [HttpGet("ready")]
        [HttpHead("ready")]
        public IActionResult IsReady() =>
            Ok(new { status = "ready", timestamp = DateTime.UtcNow });

        private static string GetUptime()
        {
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s";
        }
    }
}

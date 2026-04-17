using ForecastService.Services;
using ForecastService.Repositories;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var isRunningInContainer = string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    "true",
    StringComparison.OrdinalIgnoreCase);

// Add services
builder.Services.AddScoped<ISalesRepository, SalesRepository>();
builder.Services.AddScoped<IProductDataService, ProductDataService>();
builder.Services.AddScoped<ITimeSeriesAnalyzer, TimeSeriesAnalyzer>();
builder.Services.AddScoped<ISalesForecasterService, SalesForecasterService>();

// ADD RETRAINING SERVICE
builder.Services.AddScoped<IRetrainingService, RetrainingService>();

// ADD BACKGROUND WORKER FOR AUTOMATIC RETRAINING
builder.Services.AddHostedService<BackgroundRetrainingWorker>();

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InsightERP Forecast Service",
        Version = "1.0"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without the 'Bearer ' prefix)"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b =>
    {
        b.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Get logger
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");

try
{
    logger.LogInformation("════════════════════════════════════════════════════════════════");
    logger.LogInformation("Sales Forecasting API Starting");
    logger.LogInformation("════════════════════════════════════════════════════════════════");
    logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
    logger.LogInformation("Framework: .NET 9.0");
    logger.LogInformation("Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}", DateTime.UtcNow);

    // Configure middleware - IMPORTANT ORDER
    if (app.Environment.IsDevelopment())
    {
        logger.LogInformation("Development Mode - Swagger enabled");
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("v1/swagger.json", "Sales Forecasting API v1");
            c.RoutePrefix = "swagger";
        });
        logger.LogInformation("Swagger configured at: http://localhost:5005/swagger");
    }
    else
    {
        logger.LogInformation("Production Mode");
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    if (!isRunningInContainer)
    {
        app.UseHttpsRedirection();
    }
    app.UseStaticFiles();
    app.UseRouting();

    app.UseCors("AllowAll");
    logger.LogInformation("CORS Policy: AllowAll");

    app.MapControllers();
    logger.LogInformation("Controllers mapped");

    // Log retraining service initialization
    logger.LogInformation("════════════════════════════════════════════════════════════════");
    logger.LogInformation("Retraining Service Initialized");
    logger.LogInformation("  Automatic Weekly Retraining: ENABLED");
    logger.LogInformation("  Schedule: Every Sunday at 2:00 AM UTC");
    logger.LogInformation("  Background Worker: Active (checks every 60 minutes)");
    logger.LogInformation("  Manual Trigger Endpoint: POST /api/forecasting/retraining/trigger");
    logger.LogInformation("  Status Endpoint: GET /api/forecasting/retraining/status");
    logger.LogInformation("════════════════════════════════════════════════════════════════");

    logger.LogInformation("════════════════════════════════════════════════════════════════");
    logger.LogInformation("Application initialized successfully");
    logger.LogInformation("Base URL: http://localhost:5005");
    logger.LogInformation("Swagger UI: http://localhost:5005/swagger");
    logger.LogInformation("════════════════════════════════════════════════════════════════");

    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Fatal error during startup");
    throw;
}

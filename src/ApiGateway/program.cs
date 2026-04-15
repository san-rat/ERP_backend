using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// ── Serilog bootstrap logger ──────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/api-gateway-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("========== InsightERP API Gateway Starting ==========");

    var builder = WebApplication.CreateBuilder(args);
    var contentRootPath = builder.Environment.ContentRootPath;
    var environmentName = builder.Environment.EnvironmentName;
    var environmentSpecificOcelotFile = $"ocelot.{environmentName}.json";
    var selectedOcelotFile = File.Exists(Path.Combine(contentRootPath, environmentSpecificOcelotFile))
        ? environmentSpecificOcelotFile
        : "ocelot.json";

    builder.Host.UseSerilog();

    // ── Configuration ─────────────────────────────────────────────────────────
    builder.Configuration
        .SetBasePath(contentRootPath)
        .AddJsonFile("appsettings.json",                                          optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{environmentName}.json",                       optional: true)
        .AddJsonFile(selectedOcelotFile,                                          optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    Log.Information("Environment       → {EnvironmentName}", environmentName);
    Log.Information("Ocelot Config     → {OcelotConfigFile}", selectedOcelotFile);

    // ── JWT settings ──────────────────────────────────────────────────────────
    var jwtSecret   = Environment.GetEnvironmentVariable("JWT_SECRET")
                      ?? builder.Configuration["JwtSettings:SecretKey"]
                      ?? "your-super-secret-key-change-in-production-at-least-32-chars!!";

    var jwtIssuer   = builder.Configuration["JwtSettings:Issuer"]   ?? "InsightERP";
    var jwtAudience = builder.Configuration["JwtSettings:Audience"]  ?? "InsightERP-Users";

    // ── JWT Bearer — validates tokens produced by AuthService ─────────────────
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer("Bearer", options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken            = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

                ValidateIssuer           = true,
                ValidIssuer              = jwtIssuer,

                ValidateAudience         = true,
                ValidAudience            = jwtAudience,

                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero
            };
        });

    // Fallback policy
    builder.Services.AddAuthorization();


    // ── Ocelot ────────────────────────────────────────────────────────────────
    builder.Services.AddOcelot(builder.Configuration);

    // ── Controllers & Swagger ─────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "InsightERP API Gateway", Version = "v1" });
    });

    builder.Services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "InsightERP API Gateway v1"));

    app.UseRouting();
    app.UseCors("AllowAll");

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapHealthChecks("/health");
        endpoints.MapHealthChecks("/Gateway/health");
        endpoints.MapHealthChecks("/Gateway/live");
        endpoints.MapHealthChecks("/Gateway/ready");
        endpoints.MapControllers();
    });

    // Ocelot MUST be registered LAST
    await app.UseOcelot();

    Log.Information("Gateway running  → http://localhost:5000");
    Log.Information("Swagger UI       → http://localhost:5000/swagger");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

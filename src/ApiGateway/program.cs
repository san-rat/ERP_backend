using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// Make OpenApi model types available without qualification


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

    builder.Host.UseSerilog();

    // ── Configuration ─────────────────────────────────────────────────────────
    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile("appsettings.json",                                          optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",   optional: true)
        .AddJsonFile("ocelot.json",                                               optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    // ── JWT settings ──────────────────────────────────────────────────────────
    // Env var JWT_SECRET takes precedence; appsettings is the fallback.
    var jwtSecret   = Environment.GetEnvironmentVariable("JWT_SECRET")
                      ?? builder.Configuration["JwtSettings:SecretKey"]
                      ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

    var jwtIssuer   = builder.Configuration["JwtSettings:Issuer"]   ?? "InsightERP";
    var jwtAudience = builder.Configuration["JwtSettings:Audience"]  ?? "InsightERP-Users";

    // ── JWT Bearer — validates tokens produced by AuthService ─────────────────
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer("Bearer", options =>
        {
            options.RequireHttpsMetadata = false;   // set true in production with TLS
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
                ClockSkew                = TimeSpan.Zero   // no grace period
            };
        });

    // Fallback policy: require authenticated user for every route that the
    // Gateway controller maps — Ocelot routes are protected via ocelot.json.
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    // ── Ocelot (static routing, no Polly / Consul) ────────────────────────────
    builder.Services.AddOcelot(builder.Configuration);

    // ── Controllers (health endpoints live here) ──────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // ── Swagger ───────────────────────────────────────────────────────────────
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "InsightERP API Gateway",
            Version     = "v1",
            Description = "Reverse proxy gateway. Authenticate via /api/auth/login first, then paste the token below."
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description  = "JWT Bearer token. Enter token only (no 'Bearer ' prefix needed — Swagger adds it).",
            Name         = "Authorization",
            In           = ParameterLocation.Header,
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ── CORS ──────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
        options.AddPolicy("AllowAll", p =>
            p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    builder.Services.AddHealthChecks();

    // ── Build ─────────────────────────────────────────────────────────────────
    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "InsightERP API Gateway v1");
        c.RoutePrefix = "swagger";
    });

    app.UseRouting();
    app.UseCors("AllowAll");

    app.UseAuthentication();   // JWT token validation
    app.UseAuthorization();    // [AllowAnonymous] / [Authorize] enforcement

    // Built-in ASP.NET Core health endpoint (no JWT required — mapped before Ocelot)
    app.MapHealthChecks("/health");

    // Gateway controller endpoints (health/live/ready/info/routes/services)
    app.MapControllers();

    // Ocelot MUST be registered LAST — it acts as a catch-all reverse proxy
    await app.UseOcelot();

    Log.Information("Gateway running  → http://localhost:5000");
    Log.Information("Swagger UI       → http://localhost:5000/swagger");
    Log.Information("Health check     → http://localhost:5000/gateway/health");

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
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── JWT settings ──────────────────────────────────────────────────────────────
// Env var JWT_SECRET takes precedence; appsettings is the fallback.
var jwtSecret   = Environment.GetEnvironmentVariable("JWT_SECRET")
                  ?? builder.Configuration["JwtSettings:SecretKey"]
                  ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

var jwtIssuer   = builder.Configuration["JwtSettings:Issuer"]   ?? "InsightERP";
var jwtAudience = builder.Configuration["JwtSettings:Audience"]  ?? "InsightERP-Users";

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// ── JWT Bearer authentication ─────────────────────────────────────────────────
// Removed as Gateway now handles JWT validation centrally.
// builder.Services.AddAuthorization();

// ── CORS — allow the Vite dev server ─────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
        policy.WithOrigins("http://localhost:5173")   // Vite default port
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger with JWT "Authorize" button ───────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "InsightERP – Authentication Service",
        Version     = "v1",
        Description = "Issues JWT tokens on successful registration or login."
    });
});

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InsightERP AuthService v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("FrontendDev");        // MUST come before auth
// Removed UseAuthentication/UseAuthorization as Gateway handles it centrally.

app.MapControllers();

app.Run();
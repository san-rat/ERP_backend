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
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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

builder.Services.AddAuthorization();

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

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description  = "Paste your JWT token (without the 'Bearer ' prefix — Swagger adds it).",
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

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InsightERP AuthService v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("FrontendDev");        // MUST come before auth
app.UseAuthentication();           // MUST come before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
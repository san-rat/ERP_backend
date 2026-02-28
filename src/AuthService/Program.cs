using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Token Revocation (singleton = lives for the lifetime of the process) ───
builder.Services.AddSingleton<TokenRevocationService>();

// ─── JWT Authentication ───────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your_super_secret_key_must_be_32_chars!!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        // Custom revocation check: reject tokens whose JTI is no longer the active one
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var revocation = context.HttpContext.RequestServices
                    .GetRequiredService<TokenRevocationService>();

                var username = context.Principal?.Identity?.Name;
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (username == null || jti == null || !revocation.IsTokenActive(username, jti))
                {
                    context.Fail("Token has been revoked — please log in again.");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ─── Swagger with JWT Authorize button ───────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthService API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste your JWT token below (without the 'Bearer ' prefix — Swagger adds it automatically).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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

// ─── Pipeline ─────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();   // must come BEFORE UseAuthorization
app.UseAuthorization();

app.MapControllers();

Console.WriteLine("AuthDb Conn = " + (builder.Configuration.GetConnectionString("AuthDb") ?? "<null>"));

app.Run();
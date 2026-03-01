# InsightERP — Production Security Best Practices

## 1. Secret Management

| What | Development | Production |
|------|------------|------------|
| **JWT Secret** | `appsettings.json` (never commit the real key!) | `JWT_SECRET` environment variable or Azure Key Vault / AWS Secrets Manager |
| **DB connection strings** | `appsettings.json` or User Secrets | Secrets Manager / environment variable |
| **Minimum key length** | 32 characters | **≥ 64 characters, randomly generated** |

```bash
# Generate a strong 64-char key (Linux/macOS)
openssl rand -base64 48

# PowerShell equivalent
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

> **Never** use the placeholder key (`your-super-secret-key-change-in-production-...`) in any environment beyond local development.

---

## 2. JWT Token Settings

| Setting | Recommended value |
|---------|------------------|
| Algorithm | HS256 (symmetric) — consider RS256 (asymmetric) for multi-tenant setups |
| Expiry | 15–60 minutes for access tokens; 7–30 days for refresh tokens |
| `ClockSkew` | `TimeSpan.Zero` — reject tokens even 1 second past expiry |
| `ValidateIssuer` | **true** |
| `ValidateAudience` | **true** |
| `ValidateLifetime` | **true** |
| `ValidateIssuerSigningKey` | **true** |

---

## 3. HTTPS

- Always set `RequireHttpsMetadata = true` in production JWT options.
- Redirect HTTP → HTTPS with `app.UseHttpsRedirection()`.
- Use HSTS in production (`app.UseHsts()`).

---

## 4. Password Storage

The current implementation uses **SHA-256** for demo purposes only.  
In production, use a slow adaptive hashing algorithm:

```bash
dotnet add package BCrypt.Net-Next
```

```csharp
// Hash on registration
string hash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: 12);

// Verify on login
bool valid = BCrypt.Net.BCrypt.Verify(plainTextPassword, storedHash);
```

---

## 5. Token Revocation / Refresh

For stateless JWT, consider:
- **Short expiry** (15 min) + **refresh token** stored server-side.
- Maintain a **blocklist** (Redis set) of revoked JTIs if immediate invalidation is needed.
- Never store tokens in `localStorage` on the client — prefer `HttpOnly` cookies.

---

## 6. CORS

Tighten CORS before going to production:

```csharp
// ❌ Development (current)
options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// ✅ Production
options.AddPolicy("Production", p =>
    p.WithOrigins("https://your-frontend.example.com")
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials());
```

---

## 7. Rate Limiting

Add rate limiting on authentication endpoints to prevent brute-force:

```csharp
// In Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit         = 5;
        o.Window              = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit          = 0;
    });
});

// On login/register endpoints
[EnableRateLimiting("auth")]
[HttpPost("login")]
public IActionResult Login(...)
```

---

## 8. Logging

- Never log raw JWT tokens or passwords.
- Log failed authentication attempts with IP, username (hashed), and timestamp.
- Use **structured logging** (Serilog, already installed in Gateway) to ship to a SIEM.

---

## 9. Role Escalation Prevention

- Validate the `role` field on registration server-side (never trust client input).
- Only Admins may assign `Admin` or `Manager` roles — block this at the register endpoint.
- Audit all role changes.

---

## 10. Secrets Rotation

- Rotate the JWT signing key periodically.
- Support **key rollover**: validate tokens signed with the old key for one expiry window before discarding it.
- Use `IssuerSigningKeyResolver` in `TokenValidationParameters` for multi-key validation.

---

## Quick Checklist for Production Deployment

- [ ] Replace placeholder secret with randomly generated ≥64-char key
- [ ] Store secret in environment variable / Vault, not appsettings
- [ ] Set `RequireHttpsMetadata = true`
- [ ] Tighten CORS to specific origins
- [ ] Replace SHA-256 with BCrypt/Argon2 for password hashing
- [ ] Add rate limiting on `/api/auth/login` and `/api/auth/register`
- [ ] Set token expiry to ≤60 minutes
- [ ] Enable HSTS in production
- [ ] Set up structured log shipping (Seq, ELK, Datadog, etc.)
- [ ] Implement refresh token flow

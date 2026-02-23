var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok("AuthService is running - and sanuk is testing - dulain is watching."));

app.MapPost("/login", (LoginRequest request) =>
{
    if (request.Username == "admin" && request.Password == "password")
    {
        return Results.Ok(new
        {
            token = "fake-jwt-token",
            message = "Login successful"
        });
    }

    return Results.Unauthorized();
});

app.Run();

record LoginRequest(string Username, string Password);
var builder = WebApplication.CreateBuilder(args);

// Add Controllers (this is the key change)
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger UI (keep it always for now, later we can limit to Development)
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// Optional: keep your debug log
Console.WriteLine("AuthDb Conn = " + (builder.Configuration.GetConnectionString("AuthDb") ?? "<null>"));

app.Run();
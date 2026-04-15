using PredictionService.ML;
using PredictionService.Repositories;
using PredictionService.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddScoped<IChurnRepository, ChurnRepository>();
builder.Services.AddScoped<IChurnPredictionService, ChurnPredictionService>();
builder.Services.AddScoped<ITrainingDataRepository, TrainingDataRepository>();
builder.Services.AddScoped<IModelRetrainingService, ModelRetrainingService>();
builder.Services.AddHostedService<WeeklyModelRetrainingBackgroundService>();
builder.Services.AddScoped<ChurnModelManager>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS - Allow React app
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseCors(); // This uses the default policy

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("v1/swagger.json", "Churn Prediction API");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthorization();
app.MapControllers();

// Test endpoint
app.MapGet("/", () => "Churn Prediction API is running!");

app.Run();

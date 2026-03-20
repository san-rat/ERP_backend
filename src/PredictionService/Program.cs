using PredictionService.ML;
using PredictionService.Repositories;
using PredictionService.Services;
using Microsoft.OpenApi.Models;
var builder = WebApplication.CreateBuilder(args);

// ════════════════════════════════════════════════════════════
// ADD SERVICES
// ════════════════════════════════════════════════════════════

// Register repository and service with SINGLE database
builder.Services.AddScoped<IChurnRepository, ChurnRepository>();
builder.Services.AddScoped<IChurnPredictionService, ChurnPredictionService>();
builder.Services.AddScoped<ITrainingDataRepository, TrainingDataRepository>();
builder.Services.AddScoped<IModelRetrainingService, ModelRetrainingService>();
builder.Services.AddHostedService<WeeklyModelRetrainingBackgroundService>();
builder.Services.AddScoped<ChurnModelManager>();

builder.Services.AddControllers();

// Add Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Churn Prediction API (Single Database)", 
        Description = "ML-based churn prediction"
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b =>
    {
        b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Churn Prediction API");
        //c.RoutePrefix = string.Empty;
    });
}

app.UseCors("AllowAll");
app.MapControllers();
app.Run();
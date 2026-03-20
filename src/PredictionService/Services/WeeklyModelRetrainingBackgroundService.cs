namespace PredictionService.Services;

public class WeeklyModelRetrainingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeeklyModelRetrainingBackgroundService> _logger;
    private readonly TimeSpan _retrainingTime = new TimeSpan(9, 0, 0); // 9:00 AM Monday

    public WeeklyModelRetrainingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<WeeklyModelRetrainingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Weekly model retraining service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var nextMonday = GetNextMonday();
                var timeUntilRetrain = nextMonday.Subtract(now);

                _logger.LogInformation("⏰ Next scheduled retraining: {NextTime} (in {Hours} hours)", 
                    nextMonday, timeUntilRetrain.TotalHours);

                await Task.Delay(timeUntilRetrain, stoppingToken);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var retrainingService = scope.ServiceProvider
                        .GetRequiredService<IModelRetrainingService>();

                    _logger.LogInformation("🔄 Starting AUTOMATIC weekly model retraining...");
                    var result = await retrainingService.RetrainModelAsync();

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "✅ Weekly retraining completed! Accuracy: {Accuracy:P2}, Records: {Count}", 
                            result.Accuracy, result.RecordsUsed);
                    }
                    else
                    {
                        _logger.LogWarning("❌ Weekly retraining failed: {Message}", result.Message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Retraining service cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in weekly retraining service");
            }
        }
    }

    private DateTime GetNextMonday()
    {
        var today = DateTime.Now.Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7;

        var nextMonday = today.AddDays(daysUntilMonday).Add(_retrainingTime);
        
        if (nextMonday < DateTime.Now)
        {
            nextMonday = nextMonday.AddDays(7);
        }

        return nextMonday;
    }
}
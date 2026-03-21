namespace ForecastService.Services
{
    /// <summary>
    /// Background service that runs retraining on a schedule
    /// Runs every hour and checks if it's time to retrain
    /// </summary>
    public class BackgroundRetrainingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundRetrainingWorker> _logger;
        private const int CheckIntervalMinutes = 60; // Check every hour

        public BackgroundRetrainingWorker(
            IServiceProvider serviceProvider,
            ILogger<BackgroundRetrainingWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "🎯 Background Retraining Worker started at {Time:yyyy-MM-dd HH:mm:ss.fff zzz}",
                DateTime.UtcNow);

            // Initial delay to let app fully initialize
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var retrainingService = scope.ServiceProvider
                        .GetRequiredService<IRetrainingService>();

                    // Check if it's time to retrain
                    if (retrainingService is RetrainingService service && service.ShouldRetrain())
                    {
                        _logger.LogInformation(
                            "⏰ Scheduled retraining time detected at {Time:yyyy-MM-dd HH:mm:ss}",
                            DateTime.UtcNow);

                        // Trigger automatic retraining
                        await retrainingService.TriggerRetrainingAsync("Automatic Weekly Retrain");
                    }

                    // Wait before checking again
                    await Task.Delay(
                        TimeSpan.FromMinutes(CheckIntervalMinutes),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Background retraining worker is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "❌ Error in background retraining worker at {Time:yyyy-MM-dd HH:mm:ss}",
                        DateTime.UtcNow);

                    // Wait before retrying on error
                    await Task.Delay(
                        TimeSpan.FromMinutes(5),
                        stoppingToken);
                }
            }

            _logger.LogInformation("🛑 Background Retraining Worker stopped");
        }
    }
}
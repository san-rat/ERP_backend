using ForecastService.Models;

namespace ForecastService.Services
{
    public interface IRetrainingService
    {
        /// <summary>
        /// Manually trigger retraining for all products
        /// </summary>
        Task<RetrainingResult> TriggerRetrainingAsync(string? triggerReason = null);

        /// <summary>
        /// Retrain a specific product
        /// </summary>
        Task<bool> RetrainProductAsync(Guid productId);

        /// <summary>
        /// Get the status of the last retraining
        /// </summary>
        Task<RetrainingStatus?> GetRetrainingStatusAsync();

        /// <summary>
        /// Check if retraining is currently in progress
        /// </summary>
        bool IsRetrainingInProgress { get; }

        /// <summary>
        /// Get retraining history
        /// </summary>
        Task<List<RetrainingHistory>> GetRetrainingHistoryAsync(int limit = 10);

        /// <summary>
        /// Enable/disable automatic weekly retraining
        /// </summary>
        void SetAutoRetrainingEnabled(bool enabled);

        /// <summary>
        /// Set the day of week for automatic retraining
        /// </summary>
        void SetAutoRetrainingDay(DayOfWeek day);
    }
}
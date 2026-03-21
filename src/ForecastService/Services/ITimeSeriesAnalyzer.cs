namespace ForecastService.Services
{
    public interface ITimeSeriesAnalyzer
    {
        decimal[] CalculateMovingAverage(decimal[] data, int period);
        decimal[] CalculateExponentialSmoothing(decimal[] data, decimal alpha);
        decimal CalculateTrend(decimal[] data);
        decimal CalculateSeasonality(decimal[] data, int seasonLength);
        (decimal MAPE, decimal RMSE, decimal R_Squared) CalculateMetrics(decimal[] actual, decimal[] predicted);
    }
}
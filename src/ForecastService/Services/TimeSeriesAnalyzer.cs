namespace ForecastService.Services
{
    public class TimeSeriesAnalyzer : ITimeSeriesAnalyzer
    {
        private readonly ILogger<TimeSeriesAnalyzer> _logger;

        public TimeSeriesAnalyzer(ILogger<TimeSeriesAnalyzer> logger)
        {
            _logger = logger;
        }

        public decimal[] CalculateMovingAverage(decimal[] data, int period)
        {
            if (data.Length < period) return data;

            var result = new decimal[data.Length - period + 1];
            for (int i = 0; i <= data.Length - period; i++)
            {
                result[i] = data.Skip(i).Take(period).Average();
            }
            return result;
        }

        public decimal[] CalculateExponentialSmoothing(decimal[] data, decimal alpha)
        {
            if (data.Length == 0) return data;
            if (alpha < 0 || alpha > 1) alpha = 0.3m;

            var result = new decimal[data.Length];
            result[0] = data[0];

            for (int i = 1; i < data.Length; i++)
            {
                result[i] = alpha * data[i] + (1 - alpha) * result[i - 1];
            }
            return result;
        }

        public decimal CalculateTrend(decimal[] data)
        {
            if (data.Length < 2) return 0;

            var x = Enumerable.Range(0, data.Length).Select(i => (decimal)i).ToArray();
            var n = data.Length;
            var sumX = x.Sum();
            var sumY = data.Sum();
            var sumXY = x.Zip(data, (xi, yi) => xi * yi).Sum();
            var sumX2 = x.Select(xi => xi * xi).Sum();

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return slope;
        }

        public decimal CalculateSeasonality(decimal[] data, int seasonLength)
        {
            if (data.Length < seasonLength) return 0;

            try
            {
                var avg = data.Average();
                var variance = data.Average(x => (x - avg) * (x - avg));

                var seasonalAverages = new List<decimal>();
                for (int s = 0; s < seasonLength; s++)
                {
                    var seasonalValues = new List<decimal>();
                    for (int i = s; i < data.Length; i += seasonLength)
                    {
                        seasonalValues.Add(data[i]);
                    }

                    if (seasonalValues.Count > 0)
                    {
                        seasonalAverages.Add(seasonalValues.Average());
                    }
                }

                var seasonalVariance = seasonalAverages.Average(x => (x - avg) * (x - avg));
                var seasonalityRatio = seasonalVariance / (variance > 0 ? variance : 1);

                return Math.Min((decimal)seasonalityRatio, 1);
            }
            catch
            {
                return 0;
            }
        }

        public (decimal MAPE, decimal RMSE, decimal R_Squared) CalculateMetrics(decimal[] actual, decimal[] predicted)
        {
            if (actual.Length != predicted.Length || actual.Length == 0) return (0, 0, 0);

            try
            {
                var mapeSum = 0m;
                var validCount = 0;
                for (int i = 0; i < actual.Length; i++)
                {
                    if (actual[i] != 0)
                    {
                        mapeSum += Math.Abs((actual[i] - predicted[i]) / actual[i]);
                        validCount++;
                    }
                }
                var mape = validCount > 0 ? (mapeSum / validCount) * 100 : 0;

                var rmsSum = 0m;
                for (int i = 0; i < actual.Length; i++)
                {
                    var error = actual[i] - predicted[i];
                    rmsSum += error * error;
                }
                var rmse = (decimal)Math.Sqrt((double)(rmsSum / actual.Length));

                var actualMean = actual.Average();
                var ssTot = actual.Select(x => (x - actualMean) * (x - actualMean)).Sum();
                var ssRes = Enumerable.Range(0, actual.Length)
                    .Select(i => (actual[i] - predicted[i]) * (actual[i] - predicted[i]))
                    .Sum();
                
                var r2 = ssTot > 0 ? 1 - (ssRes / ssTot) : 0;

                return (Math.Min(mape, 100), rmse, Math.Max(Math.Min(r2, 1), 0));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating metrics");
                return (0, 0, 0);
            }
        }
    }
}
using Confluent.Kafka;

namespace OrderService.Messaging
{
    public class KafkaProducer : IKafkaProducer
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<KafkaProducer> _logger;

        public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task PublishAsync(string topic, string key, string message)
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"];

            if (string.IsNullOrWhiteSpace(bootstrapServers))
            {
                _logger.LogWarning("Kafka bootstrap servers are not configured. Skipping event publish.");
                return;
            }

            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            using var producer = new ProducerBuilder<string, string>(config).Build();

            try
            {
                var result = await producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = key,
                    Value = message
                });

                _logger.LogInformation("Kafka message sent to topic {Topic}, partition {Partition}, offset {Offset}",
                    topic, result.Partition, result.Offset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while publishing Kafka message");
            }
        }
    }
}
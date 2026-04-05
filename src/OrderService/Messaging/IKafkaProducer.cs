namespace OrderService.Messaging
{
    public interface IKafkaProducer
    {
        Task PublishAsync(string topic, string key, string message);
    }
}
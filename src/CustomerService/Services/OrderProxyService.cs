using CustomerService.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace CustomerService.Services
{
    public class OrderProxyService : IOrderProxyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OrderProxyService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> CreateOrderAsync(object payload)
        {
            var baseUrl = _configuration["ServiceEndpoints:OrderService"];
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync($"{baseUrl}/api/orders", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
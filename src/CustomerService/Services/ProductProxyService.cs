using CustomerService.Services.Interfaces;

namespace CustomerService.Services
{
    public class ProductProxyService : IProductProxyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ProductProxyService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetProductsAsync()
        {
            var baseUrl = _configuration["ServiceEndpoints:ProductService"];
            var response = await _httpClient.GetAsync($"{baseUrl}/api/products");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetProductByIdAsync(string id)
        {
            var baseUrl = _configuration["ServiceEndpoints:ProductService"];
            var response = await _httpClient.GetAsync($"{baseUrl}/api/products/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
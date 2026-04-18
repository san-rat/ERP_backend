using CustomerService.Common.Exceptions;
using CustomerService.DTOs;
using CustomerService.DTOs.Products;
using CustomerService.Services.Interfaces;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CustomerService.Services
{
    public class ProductProxyService : IProductProxyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public ProductProxyService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<PaginatedResponse<CommerceProductDto>> GetProductsAsync(int pageNumber, int pageSize, string? category, int? categoryId, string? name)
        {
            var query = new List<string>
            {
                $"pageNumber={pageNumber}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(category))
            {
                query.Add($"category={Uri.EscapeDataString(category)}");
            }

            if (categoryId.HasValue)
            {
                query.Add($"categoryId={categoryId.Value}");
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                query.Add($"name={Uri.EscapeDataString(name)}");
            }

            var response = await SendAsync(HttpMethod.Get, $"/api/internal/products?{string.Join("&", query)}");
            return await ReadRequiredAsync<PaginatedResponse<CommerceProductDto>>(response, "Unable to read products response.");
        }

        public async Task<CommerceProductDto?> GetProductByIdAsync(Guid id)
        {
            var response = await SendAsync(HttpMethod.Get, $"/api/internal/products/{id}", allowNotFound: true);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            return await ReadRequiredAsync<CommerceProductDto>(response, $"Unable to read product {id}.");
        }

        public async Task<IReadOnlyDictionary<Guid, CommerceProductDto>> ResolveProductsAsync(IEnumerable<Guid> productIds)
        {
            var distinctIds = productIds.Distinct().ToArray();
            if (distinctIds.Length == 0)
            {
                return new Dictionary<Guid, CommerceProductDto>();
            }

            var response = await SendAsync(
                HttpMethod.Post,
                "/api/internal/products/resolve",
                JsonContent.Create(new ResolveProductsRequestDto { ProductIds = distinctIds }));

            var products = await ReadRequiredAsync<List<CommerceProductDto>>(response, "Unable to resolve cart products.");
            return products.ToDictionary(product => product.Id);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, HttpContent? content = null, bool allowNotFound = false)
        {
            var baseUrl = _configuration["ServiceEndpoints:ProductService"]
                ?? throw new InvalidOperationException("ProductService endpoint is not configured.");
            var headerName = _configuration["InternalServiceAuth:HeaderName"] ?? "X-Internal-Service-Key";
            var serviceKey = _configuration["InternalServiceAuth:ServiceKey"]
                ?? throw new InvalidOperationException("Internal service key is not configured.");

            using var request = new HttpRequestMessage(method, $"{baseUrl.TrimEnd('/')}{relativePath}");
            request.Headers.TryAddWithoutValidation(headerName, serviceKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
            {
                return response;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new HttpResponseException((int)response.StatusCode, "Product service request failed.", body);
            }

            return response;
        }

        private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response, string errorMessage)
        {
            var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            if (payload is null)
            {
                throw new HttpResponseException((int)response.StatusCode, errorMessage);
            }

            return payload;
        }
    }
}

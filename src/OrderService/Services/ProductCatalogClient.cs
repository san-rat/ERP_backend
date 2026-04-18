using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OrderService.Common.Exceptions;

namespace OrderService.Services
{
    public class ProductCatalogClient : IProductCatalogClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public ProductCatalogClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<IReadOnlyDictionary<Guid, ResolvedProductDto>> ResolveProductsAsync(IEnumerable<Guid> productIds)
        {
            var ids = productIds.Distinct().ToArray();
            if (ids.Length == 0)
            {
                return new Dictionary<Guid, ResolvedProductDto>();
            }

            var response = await SendAsync(
                HttpMethod.Post,
                "/api/internal/products/resolve",
                JsonContent.Create(new { productIds = ids }));

            var products = await response.Content.ReadFromJsonAsync<List<ResolvedProductDto>>(JsonOptions);
            if (products is null)
            {
                throw new BadRequestException("Unable to read product resolution response.");
            }

            return products.ToDictionary(product => product.Id);
        }

        public async Task DeductStockAsync(Guid orderId, Guid productId, int quantity)
        {
            await SendAsync(
                HttpMethod.Post,
                "/api/internal/products/deduct-stock",
                JsonContent.Create(new
                {
                    productId,
                    orderId,
                    quantity
                }));
        }

        public async Task ReleaseStockAsync(Guid orderId, Guid productId, int quantity)
        {
            await SendAsync(
                HttpMethod.Post,
                "/api/internal/products/release-stock",
                JsonContent.Create(new
                {
                    productId,
                    orderId,
                    quantity
                }));
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, HttpContent? content = null)
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
            if (!response.IsSuccessStatusCode)
            {
                await ThrowForStatusAsync(response);
            }

            return response;
        }

        private static async Task ThrowForStatusAsync(HttpResponseMessage response)
        {
            var payload = await response.Content.ReadAsStringAsync();
            var message = string.IsNullOrWhiteSpace(payload) ? "ProductService request failed." : payload;

            throw response.StatusCode switch
            {
                HttpStatusCode.BadRequest => new BadRequestException(message),
                HttpStatusCode.Conflict => new ConflictException(message),
                HttpStatusCode.NotFound => new NotFoundException(message),
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new UnauthorizedAppException(message),
                _ => new Exception(message)
            };
        }
    }
}

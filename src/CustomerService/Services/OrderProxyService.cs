using CustomerService.Common.Exceptions;
using CustomerService.DTOs.Orders;
using CustomerService.Services.Interfaces;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CustomerService.Services
{
    public class OrderProxyService : IOrderProxyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public OrderProxyService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<CommerceOrderResponseDto> CreateOrderAsync(CommerceCreateOrderRequestDto payload)
        {
            var response = await SendAsync(
                HttpMethod.Post,
                "/api/internal/orders/ecommerce",
                JsonContent.Create(payload));

            return await ReadRequiredAsync<CommerceOrderResponseDto>(response, "Unable to read ecommerce order response.");
        }

        public async Task<IReadOnlyList<CommerceOrderResponseDto>> GetOrdersByCustomerAsync(Guid customerId)
        {
            var response = await SendAsync(HttpMethod.Get, $"/api/internal/orders/by-customer/{customerId}");
            return await ReadRequiredAsync<List<CommerceOrderResponseDto>>(response, "Unable to read customer orders.");
        }

        public async Task<CommerceOrderResponseDto?> GetOrderByIdAsync(Guid orderId)
        {
            var response = await SendAsync(HttpMethod.Get, $"/api/internal/orders/{orderId}", allowNotFound: true);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            return await ReadRequiredAsync<CommerceOrderResponseDto>(response, "Unable to read order.");
        }

        public async Task<CommerceOrderResponseDto> CancelOrderAsync(Guid orderId, string? reason)
        {
            var response = await SendAsync(
                HttpMethod.Post,
                $"/api/internal/orders/{orderId}/cancel",
                JsonContent.Create(new CancelCustomerOrderRequestDto { Reason = reason }));

            return await ReadRequiredAsync<CommerceOrderResponseDto>(response, "Unable to read cancelled order response.");
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, HttpContent? content = null, bool allowNotFound = false)
        {
            var baseUrl = _configuration["ServiceEndpoints:OrderService"]
                ?? throw new InvalidOperationException("OrderService endpoint is not configured.");
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
                throw new HttpResponseException((int)response.StatusCode, "Order service request failed.", body);
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

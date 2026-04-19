using CustomerService.DTOs.Products;

namespace CustomerService.DTOs.Cart
{
    public class AddCartItemRequestDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequestDto
    {
        public int Quantity { get; set; }
    }

    public class CartItemResponseDto
    {
        public Guid ItemId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public CommerceProductDto? Product { get; set; }
        public bool ProductAvailable { get; set; }
        public string? AvailabilityMessage { get; set; }
        public decimal? LineTotal { get; set; }
    }

    public class CartResponseDto
    {
        public Guid? CartId { get; set; }
        public int TotalItems { get; set; }
        public decimal EstimatedTotal { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public IReadOnlyList<CartItemResponseDto> Items { get; set; } = Array.Empty<CartItemResponseDto>();
    }
}

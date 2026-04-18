using CustomerService.DTOs.Addresses;
using CustomerService.DTOs.Auth;
using CustomerService.DTOs.Orders;
using CustomerService.Models;

namespace CustomerService.Helpers
{
    public static class CommerceMappings
    {
        public static CustomerAccountResponseDto ToAccountResponse(this Customer customer)
        {
            return new CustomerAccountResponseDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt
            };
        }

        public static AddressResponseDto ToAddressResponse(this CustomerAddress address)
        {
            return new AddressResponseDto
            {
                Id = address.Id,
                FullName = address.FullName,
                Phone = address.Phone,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt
            };
        }

        public static CommerceShippingAddressDto ToShippingAddress(this CustomerAddress address)
        {
            return new CommerceShippingAddressDto
            {
                FullName = address.FullName,
                Phone = address.Phone,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                Country = address.Country
            };
        }

        public static CommerceOrderResponseDto ToOrderResponse(this CustomerOrderReference reference)
        {
            return new CommerceOrderResponseDto
            {
                Id = reference.ErpOrderId,
                ExternalOrderId = reference.ExternalOrderId,
                CustomerId = reference.CustomerId,
                TotalAmount = reference.TotalAmount,
                Status = reference.Status,
                Currency = reference.Currency,
                PaymentMethod = reference.PaymentMethod,
                Notes = reference.Notes,
                CreatedAt = reference.CreatedAt,
                UpdatedAt = reference.UpdatedAt,
                ShippingAddress = new CommerceShippingAddressDto
                {
                    FullName = reference.ShippingFullName,
                    Phone = reference.ShippingPhone,
                    AddressLine1 = reference.ShippingAddressLine1,
                    AddressLine2 = reference.ShippingAddressLine2,
                    City = reference.ShippingCity,
                    State = reference.ShippingState,
                    PostalCode = reference.ShippingPostalCode,
                    Country = reference.ShippingCountry
                },
                Items = reference.Items
                    .OrderBy(item => item.ProductName)
                    .Select(item => new CommerceOrderItemResponseDto
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    })
                    .ToList()
            };
        }

        public static bool TryResolveNameParts(string? firstName, string? lastName, string? fullName, out string resolvedFirstName, out string resolvedLastName)
        {
            resolvedFirstName = (firstName ?? string.Empty).Trim();
            resolvedLastName = (lastName ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(resolvedFirstName) && !string.IsNullOrWhiteSpace(resolvedLastName))
            {
                return true;
            }

            var normalizedFullName = (fullName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedFullName))
            {
                return false;
            }

            var parts = normalizedFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            resolvedFirstName = parts[0];
            resolvedLastName = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "Customer";
            return true;
        }
    }
}

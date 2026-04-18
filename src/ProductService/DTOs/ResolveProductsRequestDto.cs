using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs
{
    public class ResolveProductsRequestDto
    {
        [Required]
        public IReadOnlyList<Guid> ProductIds { get; set; } = Array.Empty<Guid>();
    }
}

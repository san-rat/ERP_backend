using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models
{
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? ParentId { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ParentId")]
        public virtual Category? Parent { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

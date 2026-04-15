using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OrderService.DTOs
{
    public class UpdateOrderStatusDto
    {
        [JsonPropertyName("newStatus")]
        [MaxLength(30)]
        public string? NewStatus { get; set; }

        // Compatibility alias for the current employee dashboard payload shape.
        [JsonPropertyName("status")]
        [MaxLength(30)]
        public string? Status { get; set; }

        // Required only when cancelling
        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        [JsonIgnore]
        public string? RequestedStatus => !string.IsNullOrWhiteSpace(NewStatus) ? NewStatus : Status;
    }
}

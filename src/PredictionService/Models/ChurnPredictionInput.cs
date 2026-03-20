using System.ComponentModel.DataAnnotations;

namespace PredictionService.Models;

public class ChurnPredictionInput
{
    [Required(ErrorMessage = "CustomerId is required")]
    public Guid CustomerId { get; set; }
}

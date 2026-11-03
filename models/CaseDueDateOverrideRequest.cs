using System.ComponentModel.DataAnnotations;

namespace Scv.Models;

public class CaseDueDateOverrideRequest
{
    public string OverrideDueDate { get; set; }

    [Required]
    public DateTime? UpdatedDate { get; set; }
}

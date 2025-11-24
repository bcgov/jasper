using System.ComponentModel.DataAnnotations;

namespace Scv.Api.Models.Timebank;

public class TimebankSummaryRequest
{
    [Required]
    public int Period { get; set; }

    public int? JudgeId { get; set; }

    public bool? IncludeLineItems { get; set; }
}
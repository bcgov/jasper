using System;
using System.ComponentModel.DataAnnotations;

namespace Scv.Api.Models.Timebank;

public class TimebankPayoutRequest
{
    [Required]
    public int Period { get; set; }

    public int? JudgeId { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [Required]
    public double Rate { get; set; }
}
using System;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.CASES)]
public class Case : EntityBase
{
    public string AppearanceId { get; set; }
    public DateTime AppearanceDate { get; set; }
    public int? JudgeId { get; set; }
    public string CourtClass { get; set; }
    public string PhysicalFileId { get; set; }
    public string CourtFileNumber { get; set; }
    public string FileNumber { get; set; }
    public int AgeInDays { get; set; }
    public string StyleOfCause { get; set; }
    public string Reason { get; set; }
    public string DueDate { get; set; }
    // ISO date-only string; never convert a judge's selection through a time zone.
    public string OverrideDueDate { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string PartId { get; set; }
    public string RestrictionCode { get; set; }
}

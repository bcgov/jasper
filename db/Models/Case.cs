using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;
using System;

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
    public string PartId { get; set; }
    public string RestrictionCode { get; set; }
}
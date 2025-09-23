using System;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.RESERVED_JUDGEMENTS)]
public class ReservedJudgement : EntityBase
{
    public int? JudgeId { get; set; }
    public DateTime AppearanceDate { get; set; }
    public string CourtClass { get; set; }
    public string CourtFileNumber { get; set; }
    public string FileNumber { get; set; }
    public string AdjudicatorLastNameFirstName { get; set; }
    public string AdjudicatorTypeDescription { get; set; }
    public string FacilityCode { get; set; }
    public string FacilityDescription { get; set; }
    public char ReservedJudgementYesNoCode { get; set; }
    public DateTime? RFJFiledDate { get; set; }
    public char RJMultiYesNoCode { get; set; }
    public char RJOutstandingYesNoCode { get; set; }
    public int AgeInDays { get; set; }
    public float AgeInMonths { get; set; }
}
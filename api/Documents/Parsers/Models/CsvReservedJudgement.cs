using CsvHelper.Configuration.Attributes;
using System;

namespace Scv.Api.Documents.Parsers.Models;

public class CsvReservedJudgement
{
    [Name("Appearance Date")]
    public DateTime AppearanceDate { get; set; }
    [Name("Court Class Category Structure Code")]
    public string CourtClass { get; set; }
    [Name("COURT_FILE_NO")]
    public string CourtFileNumber { get; set; }
    [Name("File Number")]
    public string FileNumber { get; set; }
    [Name("Adjudicator Last Name, First Name")]
    public string AdjudicatorLastNameFirstName { get; set; }
    [Name("Adjudicator Type Description")]
    public string AdjudicatorTypeDescription { get; set; }
    [Name("Facility Code")]
    public string FacilityCode { get; set; }
    [Name("Facility Description")]
    public string FacilityDescription { get; set; }
    [Name("Reserved Judgement Yes No Code")]
    public char ReservedJudgementYesNoCode { get; set; }
    [Name("RFJ Filed Date")]
    public DateTime? RFJFiledDate { get; set; }
    [Name("RJ Multi Yes No Code")]
    public char RJMultiYesNoCode { get; set; }
    [Name("RJ Outstanding Yes No Code")]
    public char RJOutstandingYesNoCode { get; set; }
    [Name("Age in Days")]
    public int AgeInDays { get; set; }
    [Name("Age in Months")]
    public float AgeInMonths { get; set; }
}
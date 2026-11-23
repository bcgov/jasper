using System;
using CsvHelper;

namespace Scv.Api.Documents.Parsers.Models;

[CsvRecord]
public partial class CsvReservedJudgement
{
    [CsvColumn("Appearance Date")]
    public DateTime AppearanceDate { get; set; }
    [CsvColumn("Court Class Category Structure Code")]
    public string CourtClass { get; set; }
    [CsvColumn("COURT_FILE_NO")]
    public string CourtFileNumber { get; set; }
    [CsvColumn("File Number")]
    public string FileNumber { get; set; }
    [CsvColumn("Adjudicator Last Name, First Name")]
    public string AdjudicatorLastNameFirstName { get; set; }
    [CsvColumn("Adjudicator Type Description")]
    public string AdjudicatorTypeDescription { get; set; }
    [CsvColumn("Facility Code")]
    public string FacilityCode { get; set; }
    [CsvColumn("Facility Description")]
    public string FacilityDescription { get; set; }
    [CsvColumn("Reserved Judgement Yes No Code")]
    public char ReservedJudgementYesNoCode { get; set; }
    [CsvColumn("RFJ Filed Date")]
    public DateTime? RFJFiledDate { get; set; }
    [CsvColumn("RJ Multi Yes No Code")]
    public char RJMultiYesNoCode { get; set; }
    [CsvColumn("RJ Outstanding Yes No Code")]
    public char RJOutstandingYesNoCode { get; set; }
    [CsvColumn("Age in Days")]
    public int AgeInDays { get; set; }
    [CsvColumn("Age in Months")]
    public float AgeInMonths { get; set; }
}

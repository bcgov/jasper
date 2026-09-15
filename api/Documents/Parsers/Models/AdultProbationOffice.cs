namespace Scv.Api.Documents.Parsers.Models;

public class AdultProbationOffice
{
    [ExcelColumn("Office Name")] public string Name { get; set; } = "";
    [ExcelColumn("Address")] public string Address1 { get; set; } = "";
    [ExcelColumn("Address 2")] public string Address2 { get; set; } = "";
    [ExcelColumn("Phone 1")] public string Phone1 { get; set; } = "";
    [ExcelColumn("Phone 1 Notes")] public string Phone1Notes { get; set; } = "";
    [ExcelColumn("Phone 2")] public string Phone2 { get; set; } = "";
    [ExcelColumn("Phone 2 Notes")] public string Phone2Notes { get; set; } = "";
    [ExcelColumn("Toll Free")] public string TollFree { get; set; } = "";
    [ExcelColumn("Toll Free Notes")] public string TollFreeNotes { get; set; } = "";
    [ExcelColumn("Staffing Notes")] public string StaffingNotes { get; set; } = "";
}

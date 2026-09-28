namespace Scv.Api.Documents.Parsers.Models;

public class YouthProbationOffice
{
    [ExcelColumn("Office Name")] public string Name { get; set; }
    [ExcelColumn("Address")] public string Address1 { get; set; }
    [ExcelColumn("Address 2")] public string Address2 { get; set; }
    [ExcelColumn("City")] public string City { get; set; }
    [ExcelColumn("Phone")] public string Phone { get; set; }
    [ExcelColumn("Phone Notes")] public string PhoneNotes { get; set; }
    [ExcelColumn("Contact Name")] public string ContactName { get; set; }
    [ExcelColumn("Staffing Notes")] public string StaffingNotes { get; set; }
}

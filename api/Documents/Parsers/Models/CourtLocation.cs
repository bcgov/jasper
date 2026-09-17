namespace Scv.Api.Documents.Parsers.Models;

public class CourtLocation
{
    [ExcelColumn("Location Name")] public string Name { get; set; } = "";
    [ExcelColumn("Location Code (4 digit)")] public string Code { get; set; } = "";
    [ExcelColumn("JASPER Name")] public string JasperName { get; set; } = "";
    [ExcelColumn("Location Path")] public string Path { get; set; } = "";
    [ExcelColumn("Court Address 1")] public string Address1 { get; set; } = "";
    [ExcelColumn("Court Address 2")] public string Address2 { get; set; } = "";
    [ExcelColumn("Location Staffed")] public string Staffed { get; set; } = "";
    [ExcelColumn("JCM Phone 1")] public string JcmPhone1 { get; set; } = "";
    [ExcelColumn("JCM Phone 1 Notes")] public string JcmPhone1Notes { get; set; } = "";
    [ExcelColumn("JCM Phone 2")] public string JcmPhone2 { get; set; } = "";
    [ExcelColumn("JCM Phone 2 Notes")] public string JcmPhone2Notes { get; set; } = "";
    [ExcelColumn("JCM Phone 3")] public string JcmPhone3 { get; set; } = "";
    [ExcelColumn("JCM Phone 3 Notes")] public string JcmPhone3Notes { get; set; } = "";
    [ExcelColumn("JCM Phone 4")] public string JcmPhone4 { get; set; } = "";
    [ExcelColumn("JCM Phone 4 Notes")] public string JcmPhone4Notes { get; set; } = "";
    [ExcelColumn("JCM Phone 5")] public string JcmPhone5 { get; set; } = "";
    [ExcelColumn("JCM Phone 5 Notes")] public string JcmPhone5Notes { get; set; } = "";
    [ExcelColumn("Email 1")] public string Email1 { get; set; } = "";
    [ExcelColumn("Email 1 Notes")] public string Email1Notes { get; set; } = "";
    [ExcelColumn("Email 2")] public string Email2 { get; set; } = "";
    [ExcelColumn("Email 2 Notes")] public string Email2Notes { get; set; } = "";
    [ExcelColumn("IAR Schedule")] public string IarSchedule { get; set; } = "";
    [ExcelColumn("FXD Schedule")] public string FxdSchedule { get; set; } = "";
    [ExcelColumn("Adult Probation Office")] public string AdultProbationOffice { get; set; } = "";
    [ExcelColumn("Youth Probation Office")] public string YouthProbationOffice { get; set; } = "";
}

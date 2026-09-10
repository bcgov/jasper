namespace Scv.Models.CourtLocation;

public class CourtLocationDto : BaseDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string AltName { get; set; }
    public string Url { get; set; }
    public string Address1 { get; set; }
    public string Address2 { get; set; }
    public string City { get; set; }
    public bool IsStaffed { get; set; }
    public string IarSchedule { get; set; }
    public string FxdSchedule { get; set; }
    public List<PhoneInfoDto> JcmPhones { get; set; } = [];
    public List<EmailInfoDto> Emails { get; set; } = [];
    public AdultProbationOfficeDto AdultProbationOffice { get; set; }
    public YouthProbationOfficeDto YouthProbationOffice { get; set; }
}

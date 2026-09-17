namespace Scv.Models.CourtLocation;

public class AdultProbationOfficeDto : ProbationOfficeDto
{
    public List<PhoneInfoDto> Phones { get; set; } = [];
    public PhoneInfoDto TollFreePhone { get; set; }
}

public class YouthProbationOfficeDto : ProbationOfficeDto
{
    public string City { get; set; }
    public string ContactName { get; set; }
    public PhoneInfoDto Phone { get; set; }
}

public class ProbationOfficeDto
{
    public string Name { get; set; }
    public string Address1 { get; set; }
    public string Address2 { get; set; }
    public string StaffingNotes { get; set; }
}

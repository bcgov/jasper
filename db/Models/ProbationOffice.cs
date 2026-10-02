using System.Collections.Generic;

namespace Scv.Db.Models;

public class AdultProbationOffice : ProbationOffice
{
    public List<PhoneInfo> Phones { get; set; } = [];
    public PhoneInfo TollFreePhone { get; set; }
}

public class YouthProbationOffice : ProbationOffice
{
    public string City { get; set; }
    public string ContactName { get; set; }
    public PhoneInfo Phone { get; set; }
}

public class ProbationOffice
{
    public string Name { get; set; }
    public string Address1 { get; set; }
    public string Address2 { get; set; }
    public string StaffingNotes { get; set; }
}

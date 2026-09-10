using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.COURT_LOCATIONS)]
public class CourtLocation : EntityBase
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
    public List<PhoneInfo> JcmPhones { get; set; } = [];
    public List<EmailInfo> Emails { get; set; } = [];
    public AdultProbationOffice AdultProbationOffice { get; set; }
    public YouthProbationOffice YouthProbationOffice { get; set; }
}

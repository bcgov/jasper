using System.Collections.Generic;

namespace Scv.Api.Models.Order;

public class PartyDto
{
    public int? PartyId { get; set; }
    public string SurnameNm { get; set; }
    public string FirstGivenNm { get; set; }
    public string SecondGivenNm { get; set; }
    public string ThirdGivenNm { get; set; }
    public string OrganizationNm { get; set; }
    public string PartyType { get; set; }
    public List<PartyRoleDto> Roles { get; set; } = [];
}

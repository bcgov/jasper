using System.Collections.Generic;

namespace Scv.Api.Models.Order;

public class CourtFileDto
{
    public string CourtFileNo { get; set; }
    public string CourtLevelCd { get; set; }
    public string CourtDivisionCd { get; set; }
    public string CourtClassCd { get; set; }
    public int? CourtLocationId { get; set; }
    public int? PhysicalFileId { get; set; }
    public string CourtLevelDesc { get; set; }
    public string CourtDivisionDesc { get; set; }
    public string CourtClassDesc { get; set; }
    public int? CourtLocationDesc { get; set; }
    public string FullFileNo { get; set; }
    public string StyleOfCause { get; set; }
    public List<PartyDto> Parties { get; set; } = [];
}

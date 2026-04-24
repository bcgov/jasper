using Newtonsoft.Json;
using Scv.Models.Helpers;

namespace Scv.Models.Order;

[JsonConverter(typeof(FlexibleNamingJsonConverter<OrderRequestDto>))]
public class OrderRequestDto
{
    public string CourtFileNo { get; set; }
    public string CourtLevelCd { get; set; }
    public string CourtDivisionCd { get; set; }
    public string CourtClassCd { get; set; }
    public double? CourtLocationId { get; set; }
    public int? PhysicalFileId { get; set; }
    public string CourtLevelDesc { get; set; }
    public string CourtDivisionDesc { get; set; }
    public string CourtClassDesc { get; set; }
    public string CourtLocationDesc { get; set; }
    public string FullFileNo { get; set; }
    public string InitiatingAppearanceId { get; set; }
    public ReferralDto Referral { get; set; }
    public List<PartyDto> Parties { get; set; } = [];
    public List<PackageDocumentDto> PackageDocuments { get; set; } = [];
    public List<RelevantCeisDocumentDto> RelevantCeisDocuments { get; set; } = [];
}


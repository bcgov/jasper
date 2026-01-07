using System.Collections.Generic;

namespace Scv.Api.Models.Order;

public class OrderDto : BaseDto
{
    public CourtFileDto CourtFile { get; set; }
    public ReferralDto Referral { get; set; }
    public List<PackageDocumentDto> PackageDocuments { get; set; } = [];
    public List<RelevantCeisDocumentDto> RelevantCeisDocuments { get; set; } = [];
}

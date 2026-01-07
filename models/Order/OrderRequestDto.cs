using Newtonsoft.Json;
using Scv.Models.Helpers;

namespace Scv.Models.Order
{

    [JsonConverter(typeof(FlexibleNamingJsonConverter<OrderRequestDto>))]
    public class OrderRequestDto
    {
        public CourtFileDto CourtFile { get; set; }
        public ReferralDto Referral { get; set; }
        public List<PackageDocumentDto> PackageDocuments { get; set; } = [];
        public List<RelevantCeisDocumentDto> RelevantCeisDocuments { get; set; } = [];
    }
}
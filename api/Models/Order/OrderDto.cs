using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Scv.Api.Helpers;

namespace Scv.Api.Models.Order;

[JsonConverter(typeof(FlexibleNamingJsonConverter<OrderDto>))]
public class OrderDto : BaseDto
{
    public CourtFileDto CourtFile { get; set; }
    public ReferralDto Referral { get; set; }
    public List<PackageDocumentDto> PackageDocuments { get; set; } = [];
    public List<RelevantCeisDocumentDto> RelevantCeisDocuments { get; set; } = [];
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

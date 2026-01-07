namespace Scv.Api.Models.Order;

public class ReferralDto
{
    public int? ReferredDocumentId { get; set; }
    public int? PackageId { get; set; }
    public string PackageCreatedBy { get; set; }
    public string ReferralDtm { get; set; }
    public string ReferralNotesTxt { get; set; }
    public int? ReferredByAgenId { get; set; }
    public int? ReferredByPartId { get; set; }
    public int? ReferredByPaasSeqNo { get; set; }
    public string ReferredByName { get; set; }
    public string DutyTypeCd { get; set; }
    public int? SentToAgenId { get; set; }
    public int? SentToPartId { get; set; }
    public int? SentToPaasSeqNo { get; set; }
    public string SentToName { get; set; }
    public string PriorityType { get; set; }
}

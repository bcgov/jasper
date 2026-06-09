namespace Scv.Models.Order;

public class ReferralDto
{
    public int? ReferredDocumentId { get; set; }
    public int? PackageId { get; set; }
    public string PackageCreatedBy { get; set; }
    public string ReferralDtm { get; set; }
    public string ReferralNotesTxt { get; set; }
    public double? ReferredByAgenId { get; set; }
    public double? ReferredByPartId { get; set; }
    public int? ReferredByPaasSeqNo { get; set; }
    public string ReferredByName { get; set; }
    public string DutyTypeCd { get; set; }
    public double? SentToAgenId { get; set; }
    public double? SentToPartId { get; set; }
    public int? SentToPaasSeqNo { get; set; }
    public string SentToName { get; set; }
    public string PriorityType { get; set; }
    public string CourtListTypeCd { get; set; }
    public bool IsPriority => PriorityTypeDescriptor.IsPriority(PriorityType);
    public string PriorityTypeDesc => PriorityTypeDescriptor.Describe(PriorityType);
}

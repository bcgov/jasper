namespace Scv.Models.Order;

public class OrderViewDto : BaseDto
{
    public int? PackageId { get; set; }
    public int? PackageDocumentId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string ReceivedDate { get; set; }
    public string ProcessedDate { get; set; }
    public string CourtClass { get; set; }
    public string CourtFileNumber { get; set; }
    public string StyleOfCause { get; set; }
    public int PhysicalFileId { get; set; }
    public OrderStatus Status { get; set; }
    public string PriorityType { get; set; }
    public string PriorityTypeDescription => GetPriorityTypeDescription(PriorityType);
    public string CourtListType { get; set; }
    public string ReferralNotes { get; set; }

    private static string GetPriorityTypeDescription(string priorityType)
    {
        return priorityType switch
        {
            "PRO" => "Protection Orders",
            "CRTD" => "Court Directed",
            "OTHR" => "Other",
            _ => string.Empty
        };
    }
}

using Scv.Db.Models;

namespace Scv.Api.Models.Order;

public class OrderViewDto : BaseDto
{
    public int PackageNumber { get; set; }
    public string ReceivedDate { get; set; }
    public string ProcessedDate { get; set; }
    public string CourtClass { get; set; }
    public string CourtFileNumber { get; set; }
    public string StyleOfCause { get; set; }
    public int PhysicalFileId { get; set; }
    public OrderStatus Status { get; set; }
}

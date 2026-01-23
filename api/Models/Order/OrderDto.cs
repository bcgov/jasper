using System;
using Scv.Db.Models;

namespace Scv.Api.Models.Order;

public class OrderDto : BaseDto
{
    public OrderRequestDto OrderRequest { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public OrderStatus Status { get; set; }
    public bool Signed { get; set; } = false;
    public string Comments { get; set; }
    public string DocumentData { get; set; }
}

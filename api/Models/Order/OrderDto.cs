using System;

namespace Scv.Api.Models.Order;

public class OrderDto : BaseDto
{
    public OrderRequestDto OrderRequest { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

using Newtonsoft.Json;
using Scv.Api.Helpers;
using Scv.Db.Models;

namespace Scv.Api.Models.Order;

[JsonConverter(typeof(FlexibleNamingJsonConverter<OrderReviewDto>))]
public class OrderReviewDto
{
    public OrderStatus Status { get; set; } = OrderStatus.Unapproved;
    public bool Signed { get; set; } = false;
    public string Comments { get; set; }
    public string DocumentData { get; set; }
}
using Newtonsoft.Json;
using Scv.Api.Helpers;
using Scv.Db.Models;

namespace Scv.Api.Models.Order;

[JsonConverter(typeof(FlexibleNamingJsonConverter<OrderReview>))]
public class OrderReview
{
    public OrderStatus Status { get; set; }
    public bool Signed { get; set; } = false;
    public string Comments { get; set; }
    public string DocumentData { get; set; }
}
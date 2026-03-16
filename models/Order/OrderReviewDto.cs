using Newtonsoft.Json;
using Scv.Models.Helpers;

namespace Scv.Models.Order
{

    [JsonConverter(typeof(FlexibleNamingJsonConverter<OrderReviewDto>))]
    public class OrderReviewDto
    {
        public OrderStatus Status { get; set; } = OrderStatus.Unapproved;
        public bool Signed { get; set; } = false;
        public string Comments { get; set; }
        public string DocumentData { get; set; }
    }
}
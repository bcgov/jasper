using Mapster;
using Scv.Api.Models.Order;
using Scv.Db.Models;

namespace Scv.Api.Infrastructure.Mappings;

public class OrderMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Order, OrderDto>();

        config.NewConfig<OrderDto, Order>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Ent_Dtm)
            .Ignore(dest => dest.Ent_UserId)
            .Ignore(dest => dest.Upd_Dtm)
            .Ignore(dest => dest.Upd_UserId);
    }
}
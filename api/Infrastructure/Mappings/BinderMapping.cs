using Mapster;
using Scv.Api.Models;
using Scv.Db.Models;

namespace Scv.Api.Infrastructure.Mappings;

public class BinderMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<BinderDto, Binder>()
            .Ignore(dest => dest.Id);
    }
}

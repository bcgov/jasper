using Mapster;
using Scv.Api.Models;
using Scv.Db.Models;

namespace Scv.Api.Mappers;

public class MappingConfiguraton : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<BinderDto, Binder>()
            .Ignore(dest => dest.Id)
            // Labels are system-generated and should only be editable in the backend
            .Ignore(dest => dest.Labels);

    }
}

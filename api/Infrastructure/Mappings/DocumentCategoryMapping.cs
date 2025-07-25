using Mapster;
using PCSSCommon.Models;
using Scv.Api.Models;
using Scv.Db.Models;

namespace Scv.Api.Infrastructure.Mappings;

public class DocumentCategoryMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PcssConfiguration, DocumentCategoryDto>()
            .Map(dest => dest.Name, src => src.Key);

        config.NewConfig<PcssConfiguration, DocumentCategory>()
            .Map(dest => dest.Name, src => src.Key)
            .Map(dest => dest.ExternalId, src => src.PcssConfigurationId);

        config.NewConfig<DocumentCategory, DocumentCategory>()
            .Ignore(dest => dest.Id);
    }
}

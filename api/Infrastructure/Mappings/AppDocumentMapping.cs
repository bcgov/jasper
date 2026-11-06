using Mapster;
using Scv.Api.Processors;
using Scv.Db.Models;
using Scv.Models.AppDocument;

namespace Scv.Api.Infrastructure.Mappings;

public class AppDocumentMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AppDocument, AppDocumentDto>();

        config.NewConfig<UploadedFile, AppDocument>()
            .Map(dest => dest.Name, src => src.FileName)
            .Map(dest => dest.ContentType, src => src.ContentType)
            .Map(dest => dest.Size, src => src.Content.Length)
            .Map(dest => dest.Data, src => src.Content);
    }
}

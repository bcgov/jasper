using Mapster;
using ScvFileMetadataDto = Scv.Models.Document.FileMetadataDto;
using TdFileMetadataDto = TDCommon.Clients.DocumentsServices.FileMetadataDto;

namespace Scv.Api.Infrastructure.Mappings;

public class TransitoryDocumentsMapping : IRegister
{
    public static void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TdFileMetadataDto, ScvFileMetadataDto>()
            .Map(dest => dest.FileName, src => src.FileName)
            .Map(dest => dest.Extension, src => src.Extension)
            .Map(dest => dest.SizeBytes, src => src.SizeBytes)
            .Map(dest => dest.CreatedUtc, src => src.CreatedUtc.UtcDateTime)
            .Map(dest => dest.RelativePath, src => src.RelativePath)
            .Map(dest => dest.MatchedRoomFolder, src => src.MatchedRoomFolder);
    }

    void IRegister.Register(TypeAdapterConfig config)
    {
        Register(config);
    }
}

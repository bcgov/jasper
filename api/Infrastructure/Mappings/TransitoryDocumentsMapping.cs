using Mapster;
using System;
using TdFileMetadataDto = TDCommon.Clients.DocumentsServices.FileMetadataDto;
using ScvFileMetadataDto = Scv.TdApi.Models.FileMetadataDto;

namespace Scv.Api.Infrastructure.Mappings;

public class TransitoryDocumentsMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TdFileMetadataDto, ScvFileMetadataDto>()
            .Map(dest => dest.FileName, src => src.FileName)
            .Map(dest => dest.Extension, src => src.Extension)
            .Map(dest => dest.SizeBytes, src => src.SizeBytes)
            .Map(dest => dest.CreatedUtc, src => src.CreatedUtc.UtcDateTime)
            .Map(dest => dest.RelativePath, src => src.RelativePath)
            .Map(dest => dest.MatchedRoomFolder, src => src.MatchedRoomFolder);
    }
}

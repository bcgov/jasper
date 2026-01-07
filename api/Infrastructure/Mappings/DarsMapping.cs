using Mapster;
using DARSCommon.Clients.LogNotesServices;
using DARSCommon.Models;
using Scv.Models.Dars;

namespace Scv.Api.Infrastructure.Mappings;

public class DarsMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Lognotes, DarsSearchResults>()
            .Map(dest => dest.Date, src => src.DateTime)
            .Map(dest => dest.LocationId, src => src.Location)
            .Map(dest => dest.CourtRoomCd, src => src.Room)
            .Map(dest => dest.Url, src => src.Url)
            .Map(dest => dest.FileName, src => src.FileName)
            .Map(dest => dest.LocationNm, src => src.LocationName);

        config.NewConfig<DARSCommon.Clients.TranscriptsServices.Documents, TranscriptDocument>()
            .Map(dest => dest.Id, src => src.Id ?? 0)
            .Map(dest => dest.OrderId, src => src.OrderId ?? 0)
            .Map(dest => dest.Description, src => src.Description ?? string.Empty)
            .Map(dest => dest.FileName, src => src.FileName ?? string.Empty)
            .Map(dest => dest.PagesComplete, src => src.PagesComplete ?? 0)
            .Map(dest => dest.StatusCodeId, src => src.StatusCodeId ?? 0)
            .Map(dest => dest.Appearances, src => src.Appearances);

        config.NewConfig<DARSCommon.Clients.TranscriptsServices.Appearances, TranscriptAppearance>()
            .Map(dest => dest.AppearanceDt, src => src.AppearanceDt ?? string.Empty)
            .Map(dest => dest.AppearanceReasonCd, src => src.AppearanceReasonCd ?? string.Empty)
            .Map(dest => dest.AppearanceTm, src => src.AppearanceTm ?? string.Empty)
            .Map(dest => dest.JustinAppearanceId, src => src.JustinAppearanceId ?? string.Empty)
            .Map(dest => dest.CeisAppearanceId, src => src.CeisAppearanceId ?? string.Empty)
            .Map(dest => dest.CourtAgencyId, src => src.CourtAgencyId ?? string.Empty)
            .Map(dest => dest.CourtRoomCd, src => src.CourtRoomCd ?? string.Empty)
            .Map(dest => dest.JudgeFullNm, src => src.JudgeFullNm ?? string.Empty)
            .Map(dest => dest.EstimatedDuration, src => src.EstimatedDuration ?? 0)
            .Map(dest => dest.EstimatedStartTime, src => src.EstimatedStartTime ?? string.Empty)
            .Map(dest => dest.FileId, src => src.FileId ?? 0)
            .Map(dest => dest.Id, src => src.Id ?? 0)
            .Map(dest => dest.IsInCamera, src => src.IsInCamera ?? false)
            .Map(dest => dest.StatusCodeId, src => src.StatusCodeId ?? 0);
    }
}
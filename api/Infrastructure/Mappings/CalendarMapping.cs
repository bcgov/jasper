using Mapster;
using Scv.Models.Calendar;
using PCSS = PCSSCommon.Models;

namespace Scv.Api.Infrastructure.Mappings;

public class CalendarMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PCSS.JudicialCalendarActivity, CalendarDayActivity>()
            .Map(dest => dest.RoomCode, src => src.CourtRoomCode)
            .Map(dest => dest.IsRemote, src => src.IsVideo);

        config.NewConfig<PCSS.JudicialCalendarAssignment, CalendarDayActivity>()
            .Map(dest => dest.IsRemote, src => src.IsVideo);

        config.NewConfig<PCSS.AdjudicatorRestriction, AdjudicatorRestriction>()
            .Map(dest => dest.FileId, src => src.JustinOrCeisId)

            .Map(dest => dest.RoomCode, src => src.CourtRoomCode);
    }
}

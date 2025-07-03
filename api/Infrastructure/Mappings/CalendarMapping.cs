using System.Collections.Generic;
using Mapster;
using Scv.Api.Models.Calendar;
using PCSS = PCSSCommon.Models;

namespace Scv.Api.Infrastructure.Mappings;

public class CalendarMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PCSS.JudicialCalendarDay, CalendarDay>();

        config.NewConfig<PCSS.JudicialCalendar, List<CalendarDay>>()
            .MapWith(src => src.Days.Adapt<List<CalendarDay>>())
            .AfterMapping((src, dest) =>
            {
                foreach (var day in dest)
                {
                    day.RotaInitials = src.RotaInitials;
                }
            });

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

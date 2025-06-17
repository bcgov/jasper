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
    }
}

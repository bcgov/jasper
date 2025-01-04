using System.Collections.Generic;
using System.Linq;
using PCSSCommon.Models;
using Scv.Api.Models.Calendar;

namespace Scv.Api.Mappers
{
    public class MappingProfile : AutoMapper.Profile
    {
        public MappingProfile()
        {
            CreateMap<JudicialCalendarDay, CalendarDay>();

            CreateMap<JudicialCalendar, List<CalendarDay>>()
                .ConvertUsing((src, dest, context) =>
                    src.Days
                    .Select(b =>
                    {
                        var c = context.Mapper.Map<CalendarDay>(b);
                        c.RotaInitials = src.RotaInitials;
                        return c;
                    })
                    .ToList());
        }
    }

}
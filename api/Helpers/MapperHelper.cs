using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using PCSSCommon.Models;
using Scv.Api.Mappers;
using Scv.Api.Models.Calendar;

namespace Scv.Api.Helpers
{
    public static class MapperHelper
    {
        // Usage
        public static List<CalendarDay> CalendarToDays(List<JudicialCalendar> listOfCalendars)
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            var mapper = new Mapper(config);

            return listOfCalendars.SelectMany(calendarDays => mapper.Map<List<CalendarDay>>(calendarDays)).ToList();
        }
    }
}

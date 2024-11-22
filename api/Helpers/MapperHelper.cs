using AutoMapper;
using System.Collections.Generic;
using Scv.Api.Models.Calendar;
using Scv.Api.Mappers;
using PCSS.Models.REST.JudicialCalendar;
using System.Linq;

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

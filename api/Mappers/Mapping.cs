using System;
using Scv.Api.Controllers;
using Scv.Api.Services;
using Scv.Api.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections.Generic;
using PCSS.Models.REST.JudicialCalendar;
using Scv.Api.Models.Calendar;
using System.Linq;

namespace Scv.Api.Mappers
{
    public class MappingProfile : AutoMapper.Profile
    {
        public MappingProfile()
        {
            CreateMap<JudicialCalendarDay, CalendarDay>();

            
            CreateMap<JudicialCalendar, List<CalendarDay>>()
                .ConvertUsing((src, dest, context) =>
                    src.Days.Select(b => {
            var c = context.Mapper.Map<CalendarDay>(b);
            c.RotaInitials = src.RotaInitials;
            return c;
        }).ToList());
        }
    }

}
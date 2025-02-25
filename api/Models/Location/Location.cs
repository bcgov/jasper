using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scv.Api.Models.Location
{
    public class Location
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string LocationId { get; set; }
        public bool? Active { get; set; }
        public ICollection<CourtRoom> CourtRooms { get; set; }
    }
}

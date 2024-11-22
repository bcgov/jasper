

namespace PCSSClient.Clients.CourtCalendarsServices
{
    using System = global::System;
    using PCSS.Models.REST.CourtCalendar;


    public partial class CourtCalendarsServicesClient
    {
        private HttpClient _httpClient;
        private Lazy<Newtonsoft.Json.JsonSerializerSettings> _settings;

        public CourtCalendarsServicesClient(System.Net.Http.HttpClient httpClient)
        {
            _httpClient = httpClient;
            _settings = new System.Lazy<Newtonsoft.Json.JsonSerializerSettings>(CreateSerializerSettings);
        }
        private Newtonsoft.Json.JsonSerializerSettings CreateSerializerSettings()
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            UpdateJsonSerializerSettings(settings);
            return settings;
        }

        public Newtonsoft.Json.JsonSerializerSettings JsonSerializerSettings { get { return _settings.Value; } }

        partial void UpdateJsonSerializerSettings(Newtonsoft.Json.JsonSerializerSettings settings);

        public async Task<System.Collections.Generic.ICollection<CourtCalendarLocation>> CourtCalendarLocationsGetAsync(string locationId, DateTime startDate, DateTime endDate, System.Threading.CancellationToken cancellationToken)
        {
            // currently using locations and grabaing activities from there
            // https://wsgw.test.jag.gov.bc.ca/courts/catsAPI/api/v2/calendar/locations?locationIds=5,6,7,8,9,11&startDate=15-Mar-2019&endDate=15-Mar-2019

            // this could give us the activities for a location, but returns error
            // https://wsgw.test.jag.gov.bc.ca/courts/catsAPI/api/calendar/locations/1/activities 
            //{
            //     "responseCd": "500",
            //     "incidentID": "30737",
            //     "errors": "An unexpected error occurred. Please inform system support.  Incident ID=30737"
            // }
                        var locationIds = locationId.Split(',').ToList();

var courtCalendarLocations = new List<CourtCalendarLocation>();

if(locationIds.Contains("5871"))
            courtCalendarLocations.Add(
    new CourtCalendarLocation
    {
        Id = 5,
        Name = "New Westminster Law Courts",
        AgencyIdentifierCode = "3581",
        RegionCode = "FRSR",
        WorkAreaSequenceNo = 2,
        IsActive = true,
        IsGroupParent = true,
        Days = new List<CourtCalendarDay>
        {
            new CourtCalendarDay
            {
                LocationId = 5871,
                    Date = DateTime.Parse("01-Nov-2024").ToString("yyyy-MM-dd"),
                PcjRequired = 1,
                PcjMinimum = 2,
                PcjMaximum = 3,
            }
        },

    });
 
 

            return courtCalendarLocations;
        }
    }
}





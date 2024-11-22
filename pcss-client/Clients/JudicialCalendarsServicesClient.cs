
namespace PCSSClient.Clients.JudicialCalendarsServices
{
    using PCSS.Models.REST.JudicialCalendar;
    using System = global::System;

    public partial class JudicialCalendarsServicesClient
    {
        private HttpClient _httpClient;
        private Lazy<Newtonsoft.Json.JsonSerializerSettings> _settings;

        public JudicialCalendarsServicesClient(System.Net.Http.HttpClient httpClient)
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

        public async Task<ICollection<JudicialCalendar>> JudicialCalendarsGetAsync(string locationId, DateTime startDate, DateTime endDate, System.Threading.CancellationToken cancellationToken)
        {
            //         [HttpGet("api/calendar/judges")]
            // public HttpResponseMessage ReadCalendars([FromUri] int[] locationIds = null, string startDate = null, string endDate = null)


            var locationIds = locationId.Split(',').ToList();

            var judicialCalendars = new List<JudicialCalendar>();
            var onlyMyEventsFlag = string.IsNullOrWhiteSpace(locationId);


            if (locationIds.Contains("5871") || onlyMyEventsFlag)
            {
                judicialCalendars.Add(
                    new JudicialCalendar
                    {
                        Id = 2,
                        RotaInitials = "MRC",
                        ParticipantId = 12346,
                        HomeLocationId = 100,
                        HomeLocationName = "Home Location",
                        RegionCode = "RC1",
                        WorkAreaSequenceNo = 1,
                        Name = "Melissa R Collins",
                        PositionTypeCode = "PTC",
                        PositionTypeDescription = "Position Type",
                        PositionCode = "PC",
                        PositionDescription = "Position",
                        PositionStatusCode = "PSC",
                        PositionStatusDescription = "Position Status",
                        IsPresider = true,
                        IsJudge = true,
                        IsAdmin = false,
                        Days = new List<JudicialCalendarDay>
                        {
                            new JudicialCalendarDay
                            {
                                JudgeId = 12346,
                                Date = "30-Nov-2024",
                                Name = "Melissa R Collins",
                                PositionTypeCode = "PCJ",
                                PositionTypeDescription = "Provincial Court Judge",
                                PositionCode = "PJ",
                                PositionDescription = "Puisne Judge",
                                PositionStatusCode = "FT",
                                PositionStatusDescription = "FT",
                                IsPresider = true,
                                IsJudge = true,
                                IsAdmin = false,
                                Assignment = new JudicialCalendarAssignment
                                {
                                    Id = 100128,
                                    JudgeId = 12346,
                                    LocationId = 0,
                                    LocationName = "100 Mile House",
                                    Date = "30-Nov-2024",
                                    ActivityCode = "TR",
                                    ActivityDisplayCode = "Trials",
                                    ActivityDescription = "Trials",
                                    IsCommentRequired = false,
                                    ActivityClassCode = "NS",
                                    ActivityClassDescription = "Non Sitting",
                                    IsVideo = true,
                                    Force = false,
                                    ActivityAm = new JudicialCalendarActivity
                                    {
                                        ActivityCode = "TR",
                                        ActivityDescription = "Trials",
                                        ActivityDisplayCode = "Trials",
                                        ActivityClassCode = "NS",
                                        ActivityClassDescription = "Non Sitting",
                                        LocationId = 5871,
                                        LocationName = "100 Mile House",
                                        CourtSittingCode = "AM",
                                        CourtRoomCode = "009"
                                    },
                                    ActivityPm = new JudicialCalendarActivity
                                    {
                                        ActivityCode = "TR",
                                        ActivityDescription = "Trials",
                                        ActivityDisplayCode = "Trials",
                                        ActivityClassCode = "M",
                                        ActivityClassDescription = "Mixed",
                                        LocationId = 5871,
                                        LocationName = "100 Mile House",
                                        CourtSittingCode = "PM",
                                        CourtRoomCode = "009"
                                    }
                                }
                            },
                            new JudicialCalendarDay
                        {
                            JudgeId = 12346,
                            Date = "28-Nov-2024",
                            Name = "Melissa R Collins",
                            PositionTypeCode = "PCJ",
                            PositionTypeDescription = "Provincial Court Judge",
                            PositionCode = "PJ",
                            PositionDescription = "Puisne Judge",
                            PositionStatusCode = "FT",
                            PositionStatusDescription = "FT",
                            IsPresider = true,
                            IsJudge = true,
                            IsAdmin = false,
                            Assignment = new JudicialCalendarAssignment
                            {
                                Id = 100125,
                                JudgeId = 12346,
                                LocationId = 0,
                                LocationName = "100 Mile House",
                                Date = "28-Nov-2024",
                                ActivityCode = "TR",
                                ActivityDisplayCode = "Trials",
                                ActivityDescription = "Trials",
                                IsCommentRequired = false,
                                ActivityClassCode = "NS",
                                ActivityClassDescription = "Non Sitting",
                                IsVideo = false,
                                Force = false,
                                ActivityAm = new JudicialCalendarActivity
                                {

                                    ActivityCode = "TR",
                                    ActivityDescription = "Trials",
                                    ActivityDisplayCode = "Trials",
                                    ActivityClassCode = "NS",
                                    ActivityClassDescription = "Non Sitting",

                                    LocationId = 5871,
                                    LocationName = "100 Mile House",
                                    CourtSittingCode = "AM",
                                    CourtRoomCode = "009"
                                },
                                ActivityPm = new JudicialCalendarActivity
                                {
                                    ActivityCode = "A",
                                    ActivityDescription = "CivApp",
                                    ActivityDisplayCode = "CivApp",
                                    ActivityClassCode = "M",
                                    ActivityClassDescription = "Mixed",
                                    LocationId = 5871,
                                    LocationName = "100 Mile House",
                                    CourtRoomCode = "009",
                                    CourtSittingCode = "PM"
                                }

                                }
                            }
                        }
                            }
                        );
 
                    }

            judicialCalendars = judicialCalendars.Where(t => t.Days.Count > 0).ToList();
            if (onlyMyEventsFlag)
                judicialCalendars = judicialCalendars.Where(t => t.RotaInitials == "MRC").ToList();

            return judicialCalendars;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Extensions;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Scv.Api.Models.Assignment;
using System.Security.Cryptography.Xml;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;


namespace Scv.Api.Services
{
    public class AssignmentService
    {
        #region Variables

        private readonly ILogger<AssignmentService> _logger;
        private readonly FileServicesClient _filesClient;
        private readonly LookupService _lookupService;
        private readonly LocationService _locationService;
        private readonly IAppCache _cache;
        private readonly IMapper _mapper;
        private readonly string _applicationCode;
        #endregion Variables

        #region Constructor

        public AssignmentService(IConfiguration configuration, ILogger<AssignmentService> logger, FileServicesClient filesClient, IMapper mapper, LookupService lookupService, LocationService locationService, IAppCache cache, ClaimsPrincipal user)
        {
            _logger = logger;
            _filesClient = filesClient;
            _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
            _cache = cache;
            _cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(configuration.GetNonEmptyValue("Caching:FileExpiryMinutes")) * 60;
            _mapper = mapper;

            _lookupService = lookupService;
            _locationService = locationService;
        }

        #endregion Constructor

        private readonly List<Assignment> _assignments = new List<Assignment>();

        /// <summary>
        /// Returns list of assignemnts for a given month and year for current user. Added a week before and after the month to show the assignments.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Assignment>> MonthlyScheduleAsync(int year, int month)
        {
            //  first day of the month and a week before the first day of the month
            var startDate = new DateTime(year, month, 1).AddDays(-7);
            // last day of the month and a week after the last day of the month
            var endDate = startDate.AddMonths(1).AddDays(-1).AddDays(7);
            var assignments = await AssignmentsAsync(startDate, endDate);
            return assignments;
        }

        /// <summary>
        ///  Returns list of assignments for a given day for current user.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Assignment>> DailyScheduleAsync(int year, int month, int day)
        {
            var startDate = new DateTime(year, month, day);
            var endDate = startDate.AddDays(1).AddTicks(-1);
            var assignments = await AssignmentsAsync(startDate, endDate);
            return assignments;
        }

        private object GetPropertyValue(Assignment assignment, string propertyName)
        {
            return assignment.GetType().GetProperty(propertyName)?.GetValue(assignment, null);
        }

        public async Task<Assignment> GetAssignmentByIdAsync(int id)
        {
            //      [HttpGet("api/calendar/judges/{judgeId}/assignments/{assignmentId}")]
            var assignmentItem = _assignments.Find(e => e.Id == id);
            return await Task.FromResult(assignmentItem);
        }

        #region Helpers

        private async Task<HttpResponseMessage> GetPCSSAssignmentAsync(int judgeId, int assignmentId)
        {
            using (var client = new HttpClient())
            {
                var url = $"api/calendar/judges/{judgeId}/assignments/{assignmentId}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                var judicialCalendar = JsonConvert.DeserializeObject<JudicialCalendar>(jsonString);
                var calendarItem = _mapper.Map<Assignment>(judicialCalendar);
                return response;
            }
        }

        public async Task<IEnumerable<Assignment>> AssignmentsAsync(
        DateTime startDate,
        DateTime endDate,
        int? judgeId = null,
        string sortBy = null,
        bool ascending = true,
        string filterByTitle = null)
        {
            // Placeholder for future implementation to fetch assignments from
            //    public List<JcScheduleVw> GetSchedules(int judgeId, DateTime startDate, DateTime endDate)
            // or  [HttpGet("api/calendar/judges/{judgeId}")]
            //  [HttpGet("api/v2/calendar/judges/{judgeId}")]
            var assignments = new List<Assignment>
            {
            new Assignment { Title = "Now", Start = DateTime.Now  },
            new Assignment { Title = "November", Start = DateTime.Now.AddMonths(1)},
            new Assignment { Title = "November2", Start = DateTime.Now.AddMonths(1).AddDays(1)},
            new Assignment { Title = "December", Start = DateTime.Now.AddMonths(2) },
            new Assignment { Title = "December2", Start = DateTime.Now.AddMonths(2).AddDays(1) },
            new Assignment { Title = "Yesterday",  Start = DateTime.Now.AddDays(-1)  },
         //   new Assignment { Title = "Call2App",  Start = DateTime.Now.AddDays(-3)  },
            new Assignment { Title = "Tomorrow", Start = DateTime.Now.AddDays(1)  }
            };
            // Assignments would be filtered by dates within the API call
            var filteredAssignments = assignments.Where(a => a.Start >= startDate && a.Start <= endDate).ToList();
            // need to add filtering by judgeId


            if (!string.IsNullOrEmpty(filterByTitle))
            {
                // Filter assignments by title if provided
                filteredAssignments = filteredAssignments.Where(a => a.Title != null && a.Title.Contains(filterByTitle, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                // Sort assignments based on the sortBy parameter
                filteredAssignments = ascending
                    ? filteredAssignments.OrderBy(a => GetPropertyValue(a, sortBy)).ToList()
                    : filteredAssignments.OrderByDescending(a => GetPropertyValue(a, sortBy)).ToList();
            }

            return await Task.FromResult(filteredAssignments);
        }
        #endregion Helpers



        public class JudicialCalendar
        {
            public List<JudicialCalendarDay> Days { get; set; }
        }

        public class JudicialCalendarDay
        {
            private JudicialCalendarAssignment _assignment;
            public int JudgeId { get; set; }
            public string Date { get; set; }
            public JudicialCalendarAssignment Assignment
            {
                get { return _assignment; }
                set
                {
                    value.JudgeId = this.JudgeId;
                    value.Date = this.Date;
                    this._assignment = value;
                }
            }
        }

        public class JudicialCalendarAssignment
        {
            public int? Id { get; set; }

            public int JudgeId { get; set; }

            public int? LocationId { get; set; }
            public string LocationName { get; set; }

            public string Date { get; set; }

            public string ActivityCode { get; set; }
            public string ActivityDisplayCode { get; set; }
            public string ActivityDescription { get; set; }
            public bool IsCommentRequired { get; set; }

            public string ActivityClassCode { get; set; }
            public string ActivityClassDescription { get; set; }

            public string Comments { get; set; }
            public bool IsVideo { get; set; }
            public int? FromLocationId { get; set; }
            public string FromLocationName { get; set; }


            // public JudicialCalendarActivity ActivityAm { get; set; }
            // public JudicialCalendarActivity ActivityPm { get; set; }

        }
    
        public class Location{
            public int? Id { get; set; }
            public string Name { get; set; }
        }
        public class Presider{
            public int? Id { get; set; }
            public string Name { get; set; }
        }
        public class Activity{
            public int? Id { get; set; }
            public string Name { get; set; }
        }
    }

}
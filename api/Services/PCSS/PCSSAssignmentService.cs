using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
//using JCCommon.Clients.FileServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models.Assignment;

namespace Scv.Api.Services
{
    public class PCSSAssignmentService
    {
        #region Variables
        private readonly IAppCache _cache;
        private readonly string _applicationCode;
        private readonly string _requestAgencyIdentifierId;
        private readonly string _requestPartId;

        #endregion Variables

        #region Constructor

        public PCSSAssignmentService(IConfiguration configuration,
            // FileServicesClient filesClient,
            IMapper mapper,
            LookupService lookupService,
            LocationService locationService,
            IAppCache cache,
            ClaimsPrincipal claimsPrincipal,
            ILoggerFactory factory
            )
        {
            _cache = cache;
            _cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(configuration.GetNonEmptyValue("Caching:FileExpiryMinutes")) * 60;

            _applicationCode = claimsPrincipal.ApplicationCode();
            _requestAgencyIdentifierId = claimsPrincipal.AgencyCode();
            _requestPartId = claimsPrincipal.ParticipantId();
        }


        #endregion Constructor

        #region Methods

        #region Assignment

        private readonly List<Assignment> _assignments = new List<Assignment>();
        public async Task<IEnumerable<Assignment>> AssignmentsAsync()
        {
            var test = await Task.FromResult(_assignments);

            var assignments = new List<Assignment>
            {
                new Assignment { Title = "MeetingApp", Name = "MeetingApp", Start = DateTime.Now, Date = DateTime.Now },
                new Assignment { Name = "Follow up", Date = DateTime.Now.AddDays(1) },
                new Assignment { Name = "Second Follow up", Date = DateTime.Now.AddDays(2) },
                new Assignment { Title = "CallApp", Name = "CallApp", Start = DateTime.Now.AddDays(-1), Date = DateTime.Now.AddDays(-1) },
                new Assignment { Title = "Call1App", Name = "Call1App", Start = DateTime.Now.AddDays(-3), Date = DateTime.Now.AddDays(-3) }
            };

            _assignments.AddRange(assignments);
            return _assignments;
        }

        #endregion Assignment

        #endregion Methods
    }
}

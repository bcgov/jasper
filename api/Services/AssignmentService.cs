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
        private readonly string _requestAgencyIdentifierId;
        private readonly string _requestPartId;
        private readonly PCSSAssignmentService _pcssAssignmentsService;

        #endregion Variables

        #region Constructor

        public AssignmentService(IConfiguration configuration, ILogger<AssignmentService> logger, FileServicesClient filesClient, IMapper mapper, LookupService lookupService, LocationService locationService, PCSSAssignmentService pcssAssignmentsService, IAppCache cache, ClaimsPrincipal user)
        {
            _logger = logger;
            _filesClient = filesClient;
            _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
            _cache = cache;
            _cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(configuration.GetNonEmptyValue("Caching:FileExpiryMinutes")) * 60;
            _mapper = mapper;

            _lookupService = lookupService;
            _locationService = locationService;
            _pcssAssignmentsService = pcssAssignmentsService;
            _applicationCode = user.ApplicationCode();
            _requestAgencyIdentifierId = user.AgencyCode();
            _requestPartId = user.ParticipantId();

        }

        #endregion Constructor

        private readonly List<Assignment> _assignments = new List<Assignment>();

        public async Task<IEnumerable<Assignment>> MonthlyScheduleAsync(int year, int month)
        {
            var assignments = await _pcssAssignmentsService.AssignmentsAsync();
            _assignments.Clear();
            _assignments.AddRange(assignments);
            var monthlyAssignments = _assignments.FindAll(e => e.Start.Year == year && e.Start.Month == month);
            return await Task.FromResult(monthlyAssignments);
        }

        public async Task<IEnumerable<Assignment>> GetAllAssignmentsAsync()
        {
            var assignments = await _pcssAssignmentsService.AssignmentsAsync();
            _assignments.Clear();
            _assignments.AddRange(assignments);
            return await Task.FromResult(_assignments);
        }

        public async Task<Assignment> GetAssignmentByIdAsync(int id)
        {
            var assignmentItem = _assignments.Find(e => e.Id == id);
            return await Task.FromResult(assignmentItem);
        }
    }
}
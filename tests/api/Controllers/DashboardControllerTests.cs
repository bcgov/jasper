using System.Security.Claims;
using JCCommon.Clients.FileServices;
using JCCommon.Clients.LocationServices;
using JCCommon.Clients.LookupCodeServices;
using LazyCache;
using Scv.Api.Controllers;
using Scv.Api.Helpers;
using Scv.Api.Services;
using tests.api.Helpers;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;

namespace tests.api.Controllers
{
    public class DashboardControllerTests
    {
        #region Variables

        private readonly DashboardController _controller;

        private ClaimsIdentity _identity;

        #endregion Variables

        #region Constructor

        public DashboardControllerTests()
        {
            var fileServices = new EnvironmentBuilder("FileServicesClient:Username", "FileServicesClient:Password", "FileServicesClient:Url");
            var lookupServices = new EnvironmentBuilder("LookupServicesClient:Username", "LookupServicesClient:Password", "LookupServicesClient:Url");
            var locationServices = new EnvironmentBuilder("LocationServicesClient:Username", "LocationServicesClient:Password", "LocationServicesClient:Url");
            var pcssServices = new EnvironmentBuilder("PCSSServicesClient:Username", "PCSSServicesClient:Password", "PCSSServicesClient:Url");

            var lookupServiceClient = new LookupCodeServicesClient(lookupServices.HttpClient);
            var locationServiceClient = new LocationServicesClient(locationServices.HttpClient);
            var pcssLocationServicesClient = new PCSSLocationServices.LocationServicesClient(pcssServices.HttpClient);

            var fileServicesClient = new FileServicesClient(fileServices.HttpClient);
            var lookupService = new LookupService(lookupServices.Configuration, lookupServiceClient, new CachingService());
            var locationService = new LocationService(
                locationServices.Configuration,
                locationServiceClient,
                pcssLocationServicesClient,
                new CachingService());

            var claims = new[] {
                new Claim(CustomClaimTypes.ApplicationCode, "SCV"),
                new Claim(CustomClaimTypes.JcParticipantId,  fileServices.Configuration.GetNonEmptyValue("Request:PartId")),
                new Claim(CustomClaimTypes.JcAgencyCode, fileServices.Configuration.GetNonEmptyValue("Request:AgencyIdentifierId")),
                new Claim(CustomClaimTypes.IsSupremeUser, "True"),
            };
            _identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(_identity);

            //var assignmentService = new AssignmentService(fileServices.Configuration, fileServices.LogFactory.CreateLogger<AssignmentService>(), fileServicesClient, new Mapper(), lookupService, locationService,null, new CachingService(), principal);
            //_controller = new DashboardController(assignmentService, locationService, null, null)
            //{
            //    ControllerContext = HttpResponseTest.SetupMockControllerContext(fileServices.Configuration)
            //};
        }

        #endregion Constructor
    }
}

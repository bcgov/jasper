using System;
using System.Linq;
using System.Security.Claims;
using JCCommon.Clients.FileServices;
using JCCommon.Clients.LocationServices;
using JCCommon.Clients.LookupCodeServices;
using Microsoft.Extensions.Logging;
using LazyCache;
using MapsterMapper;
using Scv.Api.Controllers;
using Scv.Api.Helpers;
using Scv.Api.Services;
using tests.api.Helpers;
using Xunit;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

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
           // var pcssServiceClient = new PCSSServicesClient(locationServices.HttpClient);

            var fileServicesClient = new FileServicesClient(fileServices.HttpClient);
            var lookupService = new LookupService(lookupServices.Configuration, lookupServiceClient, new CachingService());
           // var pcssService = new PCSSService(pcssServices.Cosnfiguration, pcssServiceClient, new CachingService());
            var locationService = new LocationService(locationServices.Configuration, locationServiceClient, new CachingService());

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

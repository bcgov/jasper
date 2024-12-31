using System;
using System.Linq;
using System.Security.Claims;
using JCCommon.Clients.FileServices;
using JCCommon.Clients.LocationServices;
using JCCommon.Clients.LookupCodeServices;
using LazyCache;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scv.Api.Controllers;
using Scv.Api.Helpers;
using Scv.Api.Services;
using tests.api.Helpers;
using Xunit;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;

namespace tests.api.Controllers
{
    public class CourtListControllerTests
    {
        #region Variables

        private readonly CourtListController _controller;

        private ClaimsIdentity _identity;

        #endregion Variables

        #region Constructor

        public CourtListControllerTests()
        {
            var fileServices = new EnvironmentBuilder("FileServicesClient:Username", "FileServicesClient:Password", "FileServicesClient:Url");
            var lookupServices = new EnvironmentBuilder("LookupServicesClient:Username", "LookupServicesClient:Password", "LookupServicesClient:Url");
            var locationServices = new EnvironmentBuilder("LocationServicesClient:Username", "LocationServicesClient:Password", "LocationServicesClient:Url");
            var pcssServices = new EnvironmentBuilder("PCSS:Username", "PCSS:Password", "PCSS:Url");
            var lookupServiceClient = new LookupCodeServicesClient(lookupServices.HttpClient);
            var locationServiceClient = new LocationServicesClient(locationServices.HttpClient);
            var fileServicesClient = new FileServicesClient(fileServices.HttpClient);
            var lookupService = new LookupService(lookupServices.Configuration, lookupServiceClient, new CachingService());
            var pcssLocationServicesClient = new PCSSLocationServices.LocationServicesClient(pcssServices.HttpClient);
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

            var courtListService = new CourtListService(fileServices.Configuration, fileServices.LogFactory.CreateLogger<CourtListService>(), fileServicesClient, new Mapper(), lookupService, locationService, new CachingService(), principal);
            _controller = new CourtListController(courtListService)
            {
                ControllerContext = HttpResponseTest.SetupMockControllerContext(fileServices.Configuration)
            };
        }

        #endregion Constructor

        [Fact]
        public async void GetCourtList()
        {
            var actionResult = await _controller.GetCourtList("4801", "101", DateTime.Parse("2016-04-04"), "CR", "4889-1");

            var courtListResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("4801", courtListResponse.CourtLocationName);
            Assert.Equal("101", courtListResponse.CourtRoomCode);
            Assert.Equal("2016-04-04", courtListResponse.CourtProceedingsDate);
        }

        [Fact]
        public async void GetCourtList_Criminal_And_Civil_Files()
        {
            SetClaimsToProvincial();
            var actionResult = await _controller.GetCourtList("4801", "101", DateTime.Parse("2015-10-22"), null, "C-996");

            var courtListResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("4801", courtListResponse.CourtLocationName);
            Assert.Equal("101", courtListResponse.CourtRoomCode);
            Assert.Equal("2015-10-22", courtListResponse.CourtProceedingsDate);
            Assert.Equal("Continuation of a trial or hearing", courtListResponse.CriminalCourtList.First().AppearanceReasonDesc);
        }

        [Fact]
        public async void GetCourtList_Criminal_Crown()
        {
            SetClaimsToProvincial();
            var actionResult = await _controller.GetCourtList("4801", "101", DateTime.Parse("2019-11-15"), null, null);

            var courtListResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal("4801", courtListResponse.CourtLocationName);
            Assert.Equal("101", courtListResponse.CourtRoomCode);
            Assert.Equal("2019-11-15", courtListResponse.CourtProceedingsDate);
            Assert.Contains(courtListResponse.CriminalCourtList, f => f.Crown.Count > 0);
        }

        [Fact]
        public async void GetCourtList_Empty()
        {
            var actionResult = await _controller.GetCourtList("4801", "101", DateTime.Parse("1999-11-15"), null, null);

            var courtListResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal(0, courtListResponse.CriminalCourtList.Count);
            Assert.Equal(0, courtListResponse.CivilCourtList.Count);
        }

        [Fact]
        public async void GetCourtList_Full_1()
        {
            SetClaimsToProvincial();
            var actionResult = await _controller.GetCourtList("4801", "101", DateTime.Parse("2019-11-15"), null, null);

            var courtListResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.Equal(10, courtListResponse.CriminalCourtList.Count);

            var targetCriminalCourtList = courtListResponse.CriminalCourtList.FirstOrDefault(ccl => ccl.AppearanceSequenceNumber == "2");
            Assert.NotNull(targetCriminalCourtList);
            Assert.Equal(1, targetCriminalCourtList.Crown.Count);
            Assert.NotNull(targetCriminalCourtList.Crown.FirstOrDefault());
            Assert.Equal("Fred Brown", targetCriminalCourtList.Crown.FirstOrDefault()?.FullName);
            Assert.Equal(true, targetCriminalCourtList.Crown.FirstOrDefault()?.Assigned);
            Assert.Equal("GBM", targetCriminalCourtList.JudgeInitials);
            Assert.Equal("R", targetCriminalCourtList.ActivityClassCd);
            Assert.Equal("Adult", targetCriminalCourtList.ActivityClassDesc);
            Assert.Equal("Judicial Interim Release", targetCriminalCourtList.AppearanceReasonDesc);
            Assert.Equal("JIR", targetCriminalCourtList.AppearanceReasonCd);
            Assert.Equal("SCHD", targetCriminalCourtList.AppearanceStatusCd);

            targetCriminalCourtList = courtListResponse.CriminalCourtList.FirstOrDefault(ccl => ccl.AppearanceSequenceNumber == "5");
            Assert.NotNull(targetCriminalCourtList);
            Assert.Equal(7, targetCriminalCourtList.HearingRestriction.Count);
            Assert.Equal("Assigned", targetCriminalCourtList.HearingRestriction.First().HearingRestrictionTypeDesc);

            targetCriminalCourtList = courtListResponse.CriminalCourtList.FirstOrDefault();
            Assert.NotNull(targetCriminalCourtList);
            Assert.True(Convert.ToInt32(targetCriminalCourtList?.CaseAgeDaysNumber) > 5000);

            targetCriminalCourtList = courtListResponse.CriminalCourtList.FirstOrDefault(ccl => ccl.AppearanceSequenceNumber == "2");
            Assert.NotNull(targetCriminalCourtList);
            Assert.True(targetCriminalCourtList.InCustody);

            var targetCivilCourtList = courtListResponse.CivilCourtList.FirstOrDefault();
            Assert.NotNull(targetCivilCourtList);
            Assert.Equal("0", targetCivilCourtList.EstimatedTimeMin);
            Assert.Equal("0", targetCivilCourtList.EstimatedTimeHour);
            Assert.Equal("APP", targetCivilCourtList.AppearanceReasonCd);
            Assert.Equal("Application", targetCivilCourtList.AppearanceReasonDesc);
            Assert.Equal("SCHD", targetCivilCourtList.AppearanceStatusCd);
            Assert.Equal("Family", targetCivilCourtList.ActivityClassDesc);
            Assert.Equal("F", targetCivilCourtList.ActivityClassCd);
        }

        [Fact]
        public async void GetCourtList_Full_4()
        {
            SetClaimsToProvincial();
            var actionResult = await _controller.GetCourtList("4801", "003", DateTime.Parse("2003-10-15"), null, null);

            var courtListResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            var targetCivilCourtList = courtListResponse.CivilCourtList.First();
            Assert.NotNull(targetCivilCourtList);
            //Assert.NotEqual("Kipper has been seized to this case.", targetCivilCourtList.FileCommentText);
            Assert.NotEqual("These are the sheriff's comments", targetCivilCourtList.SheriffCommentText);
        }

        [Fact]
        public async void GetCourtList_Full_2()
        {
            SetClaimsToSupreme();
            var actionResult = await _controller.GetCourtList("4801", "009", DateTime.Parse("2014-01-07"), null, null);

            var courtListResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);

            var targetCivilCourtList = courtListResponse.CivilCourtList.First();
            Assert.NotNull(targetCivilCourtList);
            //Assert.Equal("This is the test Judge Note", targetCivilCourtList.OutOfTownJudge);
            //Assert.Equal("This is the test Equip Note", targetCivilCourtList.SupplementalEquipment);
            //Assert.Equal("This is the test Security Note", targetCivilCourtList.SecurityRestriction);
        }


        [Fact]
        public async void GetCourtList_Full_3()
        {
            SetClaimsToProvincial();
            var actionResult = await _controller.GetCourtList("4801", "007", DateTime.Parse("2020-05-06"), null, null);

            var courtListResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            Assert.NotNull(courtListResponse.CivilCourtList.First().Document);
            Assert.Equal("PLEADINGS",courtListResponse.CivilCourtList.First().Document.First().Category);
        }

        [Fact]
        public async void GetCourtList_TrialRemark_Civil()
        {
            SetClaimsToProvincial();
            var actionResult = await _controller.GetCourtList("4801", "101", DateTime.Parse("2016-05-09"), null, null);

            var courtListResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);

            var civilCourtList = courtListResponse.CivilCourtList.FirstOrDefault(ccl => ccl.CourtListPrintSortNumber == "2");
            Assert.NotNull(civilCourtList);
            //Assert.DoesNotContain("dd These remarks are going to be very long so I can test the size of this field.", civilCourtList.TrialRemarkTxt);
        }

        [Fact]
        public async void GetCourtList_Detained()
        {
            var actionResult = await _controller.GetCourtList("7999", "009", DateTime.Parse("1997-02-04"), null, null);

            var courtListResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            var criminalCourtList = courtListResponse.CriminalCourtList.FirstOrDefault(ccl => ccl.AppearanceSequenceNumber == "2");
            Assert.NotNull(criminalCourtList);
            Assert.True(criminalCourtList.Detained);
        }

        [Fact]
        public async void GetCourtList_Civil_CfcsaFile()
        {
            SetClaimsToProvincial();
            var actionResult = await _controller.GetCourtList("7999", "001", DateTime.Parse("2003-04-08"), null, null);

            var courtListResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);
            var civilCourtList = courtListResponse.CivilCourtList.FirstOrDefault(ccl => ccl.CourtListPrintSortNumber == "2");
            Assert.NotNull(civilCourtList);
            Assert.True(civilCourtList.CfcsaFile);
        }

        [Fact]
        public async void GetCourtList_Criminal_Video()
        {
            SetClaimsToProvincial();
            var actionResult = await _controller.GetCourtList("4801", "003", DateTime.Parse("2016-02-01"), null, null);

            var courtListResponse = HttpResponseTest.CheckForValidHttpResponseAndReturnValue(actionResult);

            var hasVideo = courtListResponse.CriminalCourtList.Any(ccl => ccl.Video);
            Assert.True(hasVideo);
        }


        #region Helpers


        private void SetClaimsToProvincial()
        {
            _identity.RemoveClaim(_identity.FindFirst(CustomClaimTypes.IsSupremeUser));
            _identity.AddClaim(new Claim(CustomClaimTypes.IsSupremeUser, "False"));
            var principal = new ClaimsPrincipal(_identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };        
        }

        private void SetClaimsToSupreme()
        {
            _identity.RemoveClaim(_identity.FindFirst(CustomClaimTypes.IsSupremeUser));
            _identity.AddClaim(new Claim(CustomClaimTypes.IsSupremeUser, "True"));
            var principal = new ClaimsPrincipal(_identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        #endregion Helpers

    }
}

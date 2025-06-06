using System;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using FluentValidation;
using FluentValidation.Results;
using JCCommon.Clients.FileServices;
using JCCommon.Clients.LocationServices;
using JCCommon.Clients.LookupCodeServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.SearchDateServices;
using Scv.Api.Controllers;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models.CourtList;
using Scv.Api.Services;
using Xunit;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;
using PCSSLookupServices = PCSSCommon.Clients.LookupServices;
using PCSSReportServices = PCSSCommon.Clients.ReportServices;


namespace tests.api.Controllers
{
    public class CourtListControllerTests
    {

        #region Variables

        private readonly Faker _faker;
        private readonly Mock<IValidator<CourtListReportRequest>> _mockReportValidator;

        #endregion Variables

        #region Constructor

        public CourtListControllerTests()
        {

            _faker = new Faker();
            _mockReportValidator = new Mock<IValidator<CourtListReportRequest>>();
        }

        #endregion Constructor

        #region Helpers

        private Mock<CourtListService> SetupCourtListService()
        {
            // HttpClient setup
            var mockHandler = new Mock<HttpMessageHandler>();

            var faker = new Faker();
            var httpClient = new HttpClient(mockHandler.Object)
            {
                BaseAddress = new Uri(faker.Internet.Url())
            };

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory
                .Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // IConfiguration setup
            var mockConfig = new Mock<IConfiguration>();
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(s => s.Value).Returns(_faker.Random.Number().ToString());
            mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockSection.Object);

            // ILogger setup
            var mockLogger = new Mock<ILogger<CourtListService>>();

            // Services Client setup
            var mockJCFileServicesClient = new Mock<FileServicesClient>(MockBehavior.Strict, httpClient);
            var mockPCSSSearchDateClient = new Mock<SearchDateClient>(MockBehavior.Strict, httpClient);
            var mockJCLocationServicesClient = new Mock<LocationServicesClient>(MockBehavior.Strict, httpClient);
            var mockPCSSLocationServicesClient = new Mock<PCSSLocationServices.LocationServicesClient>(MockBehavior.Strict, httpClient);
            var mockLookupCodeServicesClient = new Mock<LookupCodeServicesClient>(MockBehavior.Strict, httpClient);
            var mockPCSSLookupServicesClient = new Mock<PCSSLookupServices.LookupServicesClient>(MockBehavior.Strict, httpClient);
            var mockPCSSReportServicesClient = new Mock<PCSSReportServices.ReportServicesClient>(MockBehavior.Strict, httpClient);

            // Cache setup
            var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

            // IMapper setup
            var config = new TypeAdapterConfig();
            config.Apply(new CalendarMapping());
            config.Apply(new LocationMapping());
            var mapper = new Mapper(config);

            // Services setup
            var mockLookupService = new Mock<LookupService>(
                MockBehavior.Strict,
                mockConfig.Object,
                mockLookupCodeServicesClient.Object,
                cachingService);
            var mockLocationService = new Mock<LocationService>(
                MockBehavior.Strict,
                mockConfig.Object,
                mockJCLocationServicesClient.Object,
                mockPCSSLocationServicesClient.Object,
                mockPCSSLookupServicesClient.Object,
                cachingService,
                mapper);

            // Setup ClaimsPrincipal
            var claims = new[] {
                new Claim(CustomClaimTypes.ApplicationCode, "SCV"),
                new Claim(CustomClaimTypes.JcParticipantId,  _faker.Random.AlphaNumeric(10)),
                new Claim(CustomClaimTypes.JcAgencyCode, _faker.Random.AlphaNumeric(10)),
                new Claim(CustomClaimTypes.IsSupremeUser, "True"),
            };
            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            var mockCourtListService = new Mock<CourtListService>(
                MockBehavior.Strict,
                mockConfig.Object,
                mockLogger.Object,
                mockJCFileServicesClient.Object,
                new MapsterMapper.Mapper(),
                mockLookupService.Object,
                mockLocationService.Object,
                mockPCSSSearchDateClient.Object,
                mockPCSSReportServicesClient.Object,
                cachingService,
                principal);

            return mockCourtListService;
        }

        #endregion Helpers

        [Fact]
        public async Task GetCourtList_ShouldReturn_OK()
        {
            var mockCourtListService = this.SetupCourtListService();
            mockCourtListService
                .Setup(c => c.GetCourtListAppearances(
                    "4801",
                    190,
                    "101",
                    DateTime.Parse("2016-04-04")))
                .ReturnsAsync(new PCSSCommon.Models.ActivityClassUsage.ActivityAppearanceResultsCollection());

            var controller = new CourtListController(mockCourtListService.Object, _mockReportValidator.Object);
            var actionResult = await controller.GetCourtList("4801", "101", DateTime.Parse("2016-04-04"));

            mockCourtListService
                .Verify(c => c.GetCourtListAppearances(
                    It.IsAny<string>(),
                    190,
                    It.IsAny<string>(),
                    It.IsAny<DateTime>()),
                    Times.Once);
        }

        [Fact]
        public async Task GenerateReport_ShoulReturnBadRequest_WhenQueryParamsIsInvalid()
        {
            var mockValidationResult = new FluentValidation.Results.ValidationResult(
            [
                new ValidationFailure(_faker.Random.Word(), _faker.Lorem.Paragraph())
            ]);
            var mockCourtListService = this.SetupCourtListService();
            _mockReportValidator
                .Setup(v => v.ValidateAsync(
                    It.IsAny<CourtListReportRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockValidationResult);

            var controller = new CourtListController(mockCourtListService.Object, _mockReportValidator.Object);
            var result = await controller.GenerateReport(new CourtListReportRequest());

            Assert.IsType<BadRequestObjectResult>(result);
            _mockReportValidator
                .Verify(v =>
                    v.ValidateAsync(
                        It.IsAny<CourtListReportRequest>(),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        [Fact]
        public async Task GenerateReport_ShoulReturnFileStreamResult()
        {
            var mockValidationResult = new FluentValidation.Results.ValidationResult();
            var fakeContentDisposition = $"inline; filename={Path.GetRandomFileName()}";
            var mockResponse = new Mock<HttpResponse>();
            var headers = new HeaderDictionary();
            mockResponse.SetupGet(r => r.Headers).Returns(headers);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.SetupGet(c => c.Response).Returns(mockResponse.Object);

            var mockCourtListService = this.SetupCourtListService();
            _mockReportValidator
                .Setup(v => v.ValidateAsync(
                    It.IsAny<CourtListReportRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockValidationResult);
            mockCourtListService
                .Setup(c => c.GenerateReportAsync(It.IsAny<CourtListReportRequest>()))
                .ReturnsAsync((new MemoryStream(), fakeContentDisposition));

            var controller = new CourtListController(mockCourtListService.Object, _mockReportValidator.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = mockHttpContext.Object
                }
            };
            var result = await controller.GenerateReport(new CourtListReportRequest());

            Assert.IsType<FileStreamResult>(result);
            _mockReportValidator
                .Verify(v =>
                    v.ValidateAsync(
                        It.IsAny<CourtListReportRequest>(),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
        }
    }
}

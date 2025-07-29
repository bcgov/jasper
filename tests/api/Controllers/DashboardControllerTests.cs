using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure;
using Scv.Api.Models.Calendar;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Controllers
{
    public class DashboardControllerTests
    {
        private readonly Faker _faker;
        private readonly DashboardController _controller;
        private readonly Mock<IDashboardService> _dashboardService;

        public DashboardControllerTests()
        {
            _faker = new Faker();
            _dashboardService = new Mock<IDashboardService>();
            var httpContext = new Mock<HttpContext>();
            var claims = new List<Claim>
            {
                new(CustomClaimTypes.JudgeId, _faker.Random.Int().ToString()),
            };

            var identity = new ClaimsIdentity(claims, _faker.Random.Word());
            var mockUser = new ClaimsPrincipal(identity);
            httpContext.Setup(c => c.User).Returns(mockUser);

            _controller = new DashboardController(_dashboardService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext.Object
                }
            };
        }

        [Fact]
        public async Task GetMySchedule_Returns_BadRequest()
        {
            _dashboardService
                .Setup(d => d.GetMyScheduleAsync(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(OperationResult<CalendarSchedule>.Failure(_faker.Lorem.Paragraph()));

            var result = await _controller.GetMySchedule(_faker.Date.ToString(), _faker.Date.ToString());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetMySchedule_Returns_Success()
        {
            _dashboardService
                .Setup(d => d.GetMyScheduleAsync(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(OperationResult<CalendarSchedule>.Success(new CalendarSchedule()));

            var result = await _controller.GetMySchedule(_faker.Date.ToString(), _faker.Date.ToString());

            Assert.IsType<OkObjectResult>(result);
        }
    }
}

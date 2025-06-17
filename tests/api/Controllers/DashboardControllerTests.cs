using System.Security.Claims;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Scv.Api.Controllers;
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

        private ClaimsIdentity _identity;

        public DashboardControllerTests()
        {
            _faker = new Faker();
            _dashboardService = new Mock<IDashboardService>();

            _controller = new DashboardController(null, null, _dashboardService.Object);
        }

        [Fact]
        public async Task GetMySchedule_Returns_BadRequest()
        {
            _dashboardService
                .Setup(d => d.GetMyScheduleAsync(
                    DashboardController.TEST_JUDGE_ID,
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
                    DashboardController.TEST_JUDGE_ID,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(OperationResult<CalendarSchedule>.Success(new CalendarSchedule()));

            var result = await _controller.GetMySchedule(_faker.Date.ToString(), _faker.Date.ToString());

            Assert.IsType<OkObjectResult>(result);
        }
    }
}

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
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
                new(CustomClaimTypes.JudgeHomeLocationId, _faker.Random.Int().ToString()),
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
                    It.IsAny<string>()))
                .ReturnsAsync(OperationResult<List<CalendarDay>>.Failure(_faker.Lorem.Paragraph()));

            var result = await _controller.GetMySchedule(_faker.Date.ToString(), _faker.Date.ToString());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetMySchedule_Returns_OK()
        {
            _dashboardService
                .Setup(d => d.GetMyScheduleAsync(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(OperationResult<List<CalendarDay>>.Success([]));

            var result = await _controller.GetMySchedule(_faker.Date.ToString(), _faker.Date.ToString());

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetTodaysSchedule_Returns_BadRequest()
        {
            _dashboardService
                .Setup(d => d.GetTodaysScheduleAsync(It.IsAny<int>()))
                .ReturnsAsync(OperationResult<CalendarDay>.Failure(_faker.Lorem.Paragraph()));

            var result = await _controller.GetTodaysSchedule();

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetTodaysSchedule_Returns_OK()
        {
            _dashboardService
                .Setup(d => d.GetTodaysScheduleAsync(It.IsAny<int>()))
                .ReturnsAsync(OperationResult<CalendarDay>.Success(new CalendarDay()));

            var result = await _controller.GetTodaysSchedule();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetCourtCalendar_Returns_BadRequest()
        {
            _dashboardService
                .Setup(d => d.GetCourtCalendarScheduleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<CourtCalendarSchedule>.Failure(_faker.Lorem.Paragraph()));

            var result = await _controller.GetCourtCalendar(_faker.Date.ToString(), _faker.Date.ToString());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetCourtCalendar_Returns_OK_And_Uses_Judge_Home_Location_Id_When_Location_Ids_Is_Missing()
        {
            var judgeId = _controller.HttpContext.User.JudgeId();
            _dashboardService
                .Setup(d => d.GetCourtCalendarScheduleAsync(
                    judgeId,
                    _controller.HttpContext.User.JudgeHomeLocationId().ToString(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(OperationResult<CourtCalendarSchedule>.Success(new CourtCalendarSchedule()));

            var result = await _controller.GetCourtCalendar(
                _faker.Date.ToString(),
                _faker.Date.ToString());

            Assert.IsType<OkObjectResult>(result);
            _dashboardService
                .Verify(d => d.GetCourtCalendarScheduleAsync(
                    judgeId,
                    _controller.HttpContext.User.JudgeHomeLocationId().ToString(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                    Times.Once);
        }

        [Fact]
        public async Task GetCourtCalendar_Returns_OK_And_Uses_Location_Ids()
        {
            var locationIds = _faker.Random.Number().ToString();
            _dashboardService
                .Setup(d => d.GetCourtCalendarScheduleAsync(
                    It.IsAny<int>(),
                    locationIds,
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(OperationResult<CourtCalendarSchedule>.Success(new CourtCalendarSchedule()));

            var result = await _controller.GetCourtCalendar(
                _faker.Date.ToString(),
                _faker.Date.ToString(),
                locationIds);

            Assert.IsType<OkObjectResult>(result);
            _dashboardService
                .Verify(d => d.GetCourtCalendarScheduleAsync(
                    It.IsAny<int>(),
                    locationIds,
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                    Times.Once);
        }
    }
}

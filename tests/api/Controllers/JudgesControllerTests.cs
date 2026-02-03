using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PCSSCommon.Models;
using Scv.Api.Controllers;
using Scv.Api.Helpers;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Controllers;

public class JudgesControllerTests
{
    private readonly Mock<IJudgeService> _mockJudgeService;
    private readonly JudgesController _controller;
    private readonly Faker _faker;

    public JudgesControllerTests()
    {
        _mockJudgeService = new Mock<IJudgeService>();
        _faker = new Faker();

        _controller = new JudgesController(_mockJudgeService.Object);
    }

    #region GetJudges Tests

    [Fact]
    public async Task GetJudges_ReturnsUnauthorized_WhenUserCannotViewOthersSchedule()
    {
        SetupUserWithPermissions(canViewOthersSchedule: false);

        var result = await _controller.GetJudges();

        Assert.IsType<UnauthorizedResult>(result);
        _mockJudgeService.Verify(s => s.GetJudges(It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task GetJudges_ReturnsOk_WhenUserCanViewOthersSchedule()
    {
        SetupUserWithPermissions(canViewOthersSchedule: true);
        var judges = CreateJudgesList(5);

        _mockJudgeService
            .Setup(s => s.GetJudges(It.IsAny<List<string>>()))
            .ReturnsAsync(judges);

        var result = await _controller.GetJudges();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedJudges = Assert.IsAssignableFrom<IEnumerable<PersonSearchItem>>(okResult.Value);
        Assert.Equal(5, returnedJudges.Count());
    }

    [Fact]
    public async Task GetJudges_CallsServiceWithCorrectPositionCodes()
    {
        SetupUserWithPermissions(canViewOthersSchedule: true);
        var judges = CreateJudgesList(3);
        List<string> capturedPositionCodes = null;

        _mockJudgeService
            .Setup(s => s.GetJudges(It.IsAny<List<string>>()))
            .Callback<List<string>>(codes => capturedPositionCodes = codes)
            .ReturnsAsync(judges);

        await _controller.GetJudges();

        Assert.NotNull(capturedPositionCodes);
        Assert.Equal(5, capturedPositionCodes.Count);
        Assert.Contains(JudgeService.CHIEF_JUDGE, capturedPositionCodes);
        Assert.Contains(JudgeService.ASSOC_CHIEF_JUDGE, capturedPositionCodes);
        Assert.Contains(JudgeService.REGIONAL_ADMIN_JUDGE, capturedPositionCodes);
        Assert.Contains(JudgeService.PUISNE_JUDGE, capturedPositionCodes);
        Assert.Contains(JudgeService.SENIOR_JUDGE, capturedPositionCodes);
    }

    [Fact]
    public async Task GetJudges_ReturnsEmptyList_WhenNoJudgesFound()
    {
        SetupUserWithPermissions(canViewOthersSchedule: true);

        _mockJudgeService
            .Setup(s => s.GetJudges(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<PersonSearchItem>());

        var result = await _controller.GetJudges();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedJudges = Assert.IsAssignableFrom<IEnumerable<PersonSearchItem>>(okResult.Value);
        Assert.Empty(returnedJudges);
    }

    [Fact]
    public async Task GetJudges_ReturnsAllJudgesWithExpectedProperties()
    {
        SetupUserWithPermissions(canViewOthersSchedule: true);
        var judges = CreateJudgesList(3);

        _mockJudgeService
            .Setup(s => s.GetJudges(It.IsAny<List<string>>()))
            .ReturnsAsync(judges);

        var result = await _controller.GetJudges();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedJudges = Assert.IsAssignableFrom<IEnumerable<PersonSearchItem>>(okResult.Value).ToList();

        Assert.All(returnedJudges, judge =>
        {
            Assert.NotEqual(0, judge.PersonId);
            Assert.NotNull(judge.FullName);
            Assert.NotNull(judge.PositionCode);
        });
    }

    [Fact]
    public async Task GetJudges_CallsServiceOnce()
    {
        SetupUserWithPermissions(canViewOthersSchedule: true);
        var judges = CreateJudgesList(2);

        _mockJudgeService
            .Setup(s => s.GetJudges(It.IsAny<List<string>>()))
            .ReturnsAsync(judges);

        await _controller.GetJudges();

        _mockJudgeService.Verify(s => s.GetJudges(It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task GetJudges_ReturnsJudgesInOrderReturnedByService()
    {
        SetupUserWithPermissions(canViewOthersSchedule: true);
        var judges = new List<PersonSearchItem>
        {
            CreateJudge("Zulu", "Alpha", JudgeService.CHIEF_JUDGE),
            CreateJudge("Alpha", "Zulu", JudgeService.PUISNE_JUDGE),
            CreateJudge("Mike", "Bravo", JudgeService.SENIOR_JUDGE)
        };

        _mockJudgeService
            .Setup(s => s.GetJudges(It.IsAny<List<string>>()))
            .ReturnsAsync(judges);

        var result = await _controller.GetJudges();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedJudges = Assert.IsAssignableFrom<IEnumerable<PersonSearchItem>>(okResult.Value).ToList();

        Assert.Equal(3, returnedJudges.Count);
        Assert.Equal("Zulu", returnedJudges[0].LastName);
        Assert.Equal("Alpha", returnedJudges[1].LastName);
        Assert.Equal("Mike", returnedJudges[2].LastName);
    }

    [Fact]
    public async Task GetJudges_OnlyIncludesSpecificPositionTypes()
    {
        SetupUserWithPermissions(canViewOthersSchedule: true);
        List<string> capturedPositionCodes = null;

        _mockJudgeService
            .Setup(s => s.GetJudges(It.IsAny<List<string>>()))
            .Callback<List<string>>(codes => capturedPositionCodes = codes)
            .ReturnsAsync(new List<PersonSearchItem>());

        await _controller.GetJudges();

        Assert.NotNull(capturedPositionCodes);
        Assert.DoesNotContain("OTHER", capturedPositionCodes);
        Assert.DoesNotContain("ADMIN", capturedPositionCodes);
        Assert.All(capturedPositionCodes, code =>
            Assert.Contains(code, new[] { "CJ", "ACJ", "RAJ", "PJ", "SJ" }));
    }

    [Fact]
    public async Task GetJudges_HandlesNullReturnFromService()
    {
        SetupUserWithPermissions(canViewOthersSchedule: true);

        _mockJudgeService
            .Setup(s => s.GetJudges(It.IsAny<List<string>>()))
            .ReturnsAsync((IEnumerable<PersonSearchItem>)null);

        var result = await _controller.GetJudges();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Null(okResult.Value);
    }

    #endregion

    #region Authorization Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetJudges_ChecksCanViewOthersSchedulePermission(bool canView)
    {
        SetupUserWithPermissions(canViewOthersSchedule: canView);
        var judges = CreateJudgesList(1);

        _mockJudgeService
            .Setup(s => s.GetJudges(It.IsAny<List<string>>()))
            .ReturnsAsync(judges);

        var result = await _controller.GetJudges();

        if (canView)
        {
            Assert.IsType<OkObjectResult>(result);
        }
        else
        {
            Assert.IsType<UnauthorizedResult>(result);
        }
    }

    [Fact]
    public async Task GetJudges_ReturnsUnauthorized_WhenUserHasNoPermissionClaim()
    {
        SetupUserWithoutPermissions();

        var result = await _controller.GetJudges();

        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region Helper Methods

    private void SetupUserWithPermissions(bool canViewOthersSchedule)
    {
        var claims = new List<Claim>();

        if (canViewOthersSchedule)
        {
            claims.Add(new Claim(CustomClaimTypes.Groups, "jasper-view-others-schedule"));
        }

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetupUserWithoutPermissions()
    {
        var identity = new ClaimsIdentity(new List<Claim>(), "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private List<PersonSearchItem> CreateJudgesList(int count)
    {
        var judges = new List<PersonSearchItem>();
        var positionCodes = new[]
        {
            JudgeService.CHIEF_JUDGE,
            JudgeService.ASSOC_CHIEF_JUDGE,
            JudgeService.REGIONAL_ADMIN_JUDGE,
            JudgeService.PUISNE_JUDGE,
            JudgeService.SENIOR_JUDGE
        };

        for (int i = 0; i < count; i++)
        {
            judges.Add(CreateJudge(
                _faker.Name.LastName(),
                _faker.Name.FirstName(),
                _faker.PickRandom(positionCodes)
            ));
        }

        return judges;
    }

    private PersonSearchItem CreateJudge(string lastName, string firstName, string positionCode)
    {
        return new PersonSearchItem
        {
            PersonId = _faker.Random.Int(1000, 9999),
            UserId = _faker.Random.Int(1000, 9999),
            LastName = lastName,
            FirstName = firstName,
            FullName = $"{firstName} {lastName}",
            PositionCode = positionCode,
            PositionDescription = GetPositionDescription(positionCode),
            HomeLocationId = _faker.Random.Int(1, 100),
            HomeLocationName = _faker.Address.City(),
            StatusCode = "A",
            StatusDescription = "Active",
            RotaInitials = _faker.Random.String2(2, "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        };
    }

    private string GetPositionDescription(string positionCode)
    {
        return positionCode switch
        {
            JudgeService.CHIEF_JUDGE => "Chief Judge",
            JudgeService.ASSOC_CHIEF_JUDGE => "Associate Chief Judge",
            JudgeService.REGIONAL_ADMIN_JUDGE => "Regional Administrative Judge",
            JudgeService.PUISNE_JUDGE => "Puisne Judge",
            JudgeService.SENIOR_JUDGE => "Senior Judge",
            _ => "Unknown"
        };
    }

    #endregion
}
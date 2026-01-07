using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Helpers;
using Scv.Api.Services;
using Scv.Core.Helpers;
using Scv.Db.Models;
using Xunit;

namespace tests.api.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ScvDbContext> _dbContext;

        private readonly Faker _faker = new();

        public AuthControllerTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _dbContext = new Mock<ScvDbContext>();
        }

        #region Unit Tests

        [Fact]
        public async Task UserInfo_ReturnsOK_And_UserTypeIDIR()
        {
            var expectedUsername = _faker.Internet.UserName();
            var expectedUserType = "idir";
            var expectedIsSupremeUser = _faker.Random.Bool();
            var expectedRole = _faker.Random.Word();
            var expectedSubRole = _faker.Random.Word();
            var expectedJcAgencyCode = _faker.Random.Word();

            var mockHttpContext = this.GetUserInfoMockedHttpContext(
                "/info",
                expectedUsername,
                expectedUserType,
                expectedIsSupremeUser,
                expectedRole,
                expectedSubRole,
                expectedJcAgencyCode);
            var mockUserService = new Mock<IUserService>();

            var controller = new AuthController(_dbContext.Object, _mockConfig.Object, null)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = mockHttpContext.Object
                }
            };

            var response = await controller.UserInfo() as OkObjectResult;
            dynamic responseObj = response.Value;

            var actualUserType = responseObj.GetType().GetProperty("UserType").GetValue(responseObj).ToString();
            var actualEnableArchive = Convert.ToBoolean(responseObj.GetType().GetProperty("EnableArchive").GetValue(responseObj).ToString());
            var actualRole = responseObj.GetType().GetProperty("ExternalRole").GetValue(responseObj).ToString();
            var actualSubRole = responseObj.GetType().GetProperty("SubRole").GetValue(responseObj).ToString();
            var actualIsSupremeUser = Convert.ToBoolean(responseObj.GetType().GetProperty("IsSupremeUser").GetValue(responseObj).ToString());
            var actualAgencyCode = responseObj.GetType().GetProperty("AgencyCode").GetValue(responseObj).ToString();
            var actualUtcNow = Convert.ToDateTime(responseObj.GetType().GetProperty("UtcNow").GetValue(responseObj).ToString());

            var isSameUserType = actualUserType == expectedUserType;
            var isSameRole = actualRole == expectedRole;
            var isSameSubRole = actualSubRole == expectedSubRole;
            var isSameIsSupremeUser = actualIsSupremeUser == expectedIsSupremeUser;
            var isSameAgencyCode = actualAgencyCode == expectedJcAgencyCode;


            Assert.IsType<OkObjectResult>(response);
            Assert.True(isSameUserType);
            Assert.False(actualEnableArchive);
            Assert.True(isSameRole);
            Assert.True(isSameSubRole);
            Assert.True(isSameIsSupremeUser);
            Assert.True(isSameAgencyCode);
            Assert.NotNull(actualUtcNow);
        }

        [Fact]
        public async Task UserInfo_ReturnsOK_And_UserTypeVC()
        {
            var expectedUserType = "vc";

            var mockHttpContext = this.GetUserInfoMockedHttpContext(
                "/info",
                _faker.Internet.UserName(),
                expectedUserType,
                _faker.Random.Bool(),
                _faker.Random.Word(),
                _faker.Random.Word(),
                _faker.Random.Word());
            var mockUserService = new Mock<IUserService>();

            var controller = new AuthController(_dbContext.Object, _mockConfig.Object, null)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = mockHttpContext.Object
                }
            };

            var response = await controller.UserInfo() as OkObjectResult;
            dynamic responseObj = response.Value;

            var actualUserType = responseObj.GetType().GetProperty("UserType").GetValue(responseObj).ToString();
            var isSameUserType = actualUserType == expectedUserType;

            Assert.IsType<OkObjectResult>(response);
            Assert.True(isSameUserType);
        }

        [Fact]
        public async Task UserInfo_ReturnsOK_And_UserTypeJudiciary()
        {
            var expectedUserType = "judiciary";

            var mockHttpContext = this.GetUserInfoMockedHttpContext(
                "/info",
                _faker.Internet.UserName(),
                _faker.Random.Word(),
                _faker.Random.Bool(),
                _faker.Random.Word(),
                _faker.Random.Word(),
                _faker.Random.Word());
            var mockUserService = new Mock<IUserService>();

            var controller = new AuthController(_dbContext.Object, _mockConfig.Object, null)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = mockHttpContext.Object
                }
            };

            var response = await controller.UserInfo() as OkObjectResult;
            dynamic responseObj = response.Value;

            var actualUserType = responseObj.GetType().GetProperty("UserType").GetValue(responseObj).ToString();
            var isSameUserType = actualUserType == expectedUserType;

            Assert.IsType<OkObjectResult>(response);
            Assert.True(isSameUserType);
        }

        #endregion

        #region Private Methods

        private Mock<HttpContext> GetUserInfoMockedHttpContext(
            string requestUrl,
            string expectedUsername,
            string expectedUserType,
            bool expectedIsSupremeUser,
            string expectedRole,
            string expectedSubRole,
            string expectedJcAgencyCode)
        {
            var mockHttpContext = new Mock<HttpContext>();

            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Path).Returns(requestUrl);
            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

            // Mock User (ClaimsPrincipal)
            var claims = new List<Claim>
            {
                new(CustomClaimTypes.PreferredUsername, $"{expectedUsername}@{expectedUserType}"),
                new(CustomClaimTypes.IsSupremeUser, expectedIsSupremeUser.ToString()),
                new(CustomClaimTypes.ExternalRole, expectedRole),
                new(CustomClaimTypes.SubRole, expectedSubRole),
                new(CustomClaimTypes.JcAgencyCode, expectedJcAgencyCode)
            };

            var identity = new ClaimsIdentity(claims, _faker.Random.Word());
            var mockUser = new ClaimsPrincipal(identity);
            mockHttpContext.Setup(c => c.User).Returns(mockUser);

            return mockHttpContext;
        }

        #endregion
    }
}


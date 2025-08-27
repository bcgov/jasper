using Bogus;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Infrastructure;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace tests.api.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IValidator<UserDto>> _mockValidator;
        private readonly Mock<ILogger<UsersController>> _mockLogger;
        private readonly Faker _faker;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockValidator = new Mock<IValidator<UserDto>>();
            _mockLogger = new Mock<ILogger<UsersController>>();
            _faker = new Faker();
        }

        private UsersController CreateControllerWithContext(IEnumerable<Claim> claims)
        {
            var controller = new UsersController(
                _mockUserService.Object,
                _mockValidator.Object,
                _mockLogger.Object
            );

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        [Fact]
        public async Task RequestAccess_ReturnsCreatedResult_WhenUserIsCreated()
        {
            // Arrange
            var email = _faker.Internet.Email();
            var userId = Guid.NewGuid().ToString();
            var givenName = _faker.Name.FirstName();
            var familyName = _faker.Name.LastName();

            var claims = new List<Claim>
            {
                new Claim("preferred_username", $"{userId}@idir"),
                new Claim("given_name", givenName),
                new Claim("family_name", familyName)
            };

            var controller = CreateControllerWithContext(claims);

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UserDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockUserService
                .Setup(s => s.AddAsync(It.IsAny<UserDto>()))
                .ReturnsAsync(OperationResult<UserDto>.Success(new UserDto { Email = email }));

            _mockUserService
                .Setup(s => s.ValidateAsync(It.IsAny<UserDto>(), It.IsAny<bool>()))
                .ReturnsAsync(OperationResult<UserDto>.Success(new UserDto { Email = email }));

            // Act
            var result = await controller.RequestAccess(email);

            // Assert
            var actionResult = Assert.IsAssignableFrom<ActionResult>(result);
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
            var userDto = Assert.IsType<UserDto>(createdResult.Value);
            Assert.Equal(email, userDto.Email);
        }

        [Fact]
        public async Task RequestAccess_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var email = _faker.Internet.Email();
            var userId = Guid.NewGuid().ToString();

            var claims = new List<Claim>
            {
                new Claim("preferred_username", $"{userId}@idir"),
                new Claim("given_name", _faker.Name.FirstName()),
                new Claim("family_name", _faker.Name.LastName())
            };

            var controller = CreateControllerWithContext(claims);

            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Email", "Invalid email")
            };

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UserDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult(validationFailures));

            // Act
            var result = await controller.RequestAccess(email);

            // Assert
            var actionResult = Assert.IsAssignableFrom<ActionResult>(result);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task RequestAccess_ReturnsCreatedResult_NoPreferredUsername()
        {
            // Arrange
            var email = _faker.Internet.Email();
            var givenName = _faker.Name.FirstName();
            var familyName = _faker.Name.LastName();

            var claims = new List<Claim>
            {
                new Claim("given_name", givenName),
                new Claim("family_name", familyName)
            };

            var controller = CreateControllerWithContext(claims);

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UserDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockUserService
                .Setup(s => s.AddAsync(It.IsAny<UserDto>()))
                .ReturnsAsync(OperationResult<UserDto>.Success(new UserDto { Email = email }));

            _mockUserService
                .Setup(s => s.ValidateAsync(It.IsAny<UserDto>(), It.IsAny<bool>()))
                .ReturnsAsync(OperationResult<UserDto>.Success(new UserDto { Email = email }));

            // Act
            var result = await controller.RequestAccess(email);

            // Assert
            var actionResult = Assert.IsAssignableFrom<ActionResult>(result);
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
            var userDto = Assert.IsType<UserDto>(createdResult.Value);
            Assert.Equal(email, userDto.Email);
        }
    }
}
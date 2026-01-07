using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure;
using Scv.Models.AccessControlManagement;
using Scv.Api.Services;
using Xunit;
using Scv.Core.Infrastructure;
using Scv.Core.Helpers;

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
        public async Task RequestAccess_ReturnsUpdatedResult_WhenUserIsCreated()
        {
            // Arrange
            var email = _faker.Internet.Email();

            var claims = new List<Claim>
            {
                new Claim(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()),
                new Claim(ClaimTypes.Email, email),
            };

            var controller = CreateControllerWithContext(claims);

            var existingUser = new UserDto { Email = "old@email.com" };

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UserDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<UserDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockUserService
                .Setup(s => s.ValidateAsync(It.IsAny<UserDto>(), It.IsAny<bool>()))
                .ReturnsAsync(OperationResult<UserDto>.Success(new UserDto { Email = email }));

            _mockUserService
                .Setup(s => s.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUser);

            _mockUserService
                .Setup(s => s.UpdateAsync(It.IsAny<UserDto>()))
                .ReturnsAsync(OperationResult<UserDto>.Success(existingUser));

            // Act
            var result = await controller.RequestAccess();

            // Assert
            var actionResult = Assert.IsAssignableFrom<ActionResult>(result);
            var updatedResult = Assert.IsType<OkObjectResult>(actionResult);
            var userDto = Assert.IsType<UserDto>(updatedResult.Value);
            Assert.Equal(email, userDto.Email);
        }

        [Fact]
        public async Task RequestAccess_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var email = _faker.Internet.Email();

            var claims = new List<Claim>
            {
                new Claim(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()),
                new Claim(ClaimTypes.Email, email),
            };

            var controller = CreateControllerWithContext(claims);

            var existingUser = new UserDto { Email = "old@email.com" };

            _mockUserService
                .Setup(s => s.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUser);

            _mockUserService
                .Setup(s => s.UpdateAsync(It.IsAny<UserDto>()))
                .ReturnsAsync(OperationResult<UserDto>.Success(existingUser));

            _mockUserService
                .Setup(s => s.ValidateAsync(It.IsAny<UserDto>(), It.IsAny<bool>()))
                .ReturnsAsync(OperationResult<UserDto>.Failure("Invalid email"));

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UserDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<UserDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            // Act
            var result = await controller.RequestAccess();

            // Assert
            var actionResult = Assert.IsAssignableFrom<ActionResult>(result);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task RequestAccess_ReturnsOkResult_WhenUserIsUpdated()
        {
            // Arrange
            var email = _faker.Internet.Email();

            var claims = new List<Claim>
            {
                new Claim(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()),
                new Claim(ClaimTypes.Email, email),
            };

            var controller = CreateControllerWithContext(claims);

            var existingUser = new UserDto { Email = "old@email.com" };

            // Mock base.GetById to return existing user
            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UserDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<UserDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockUserService
                .Setup(s => s.ValidateAsync(It.IsAny<UserDto>(), It.IsAny<bool>()))
                .ReturnsAsync(OperationResult<UserDto>.Success(new UserDto { Email = email }));

            _mockUserService
                .Setup(s => s.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUser);

            _mockUserService
                .Setup(s => s.UpdateAsync(It.IsAny<UserDto>()))
                .ReturnsAsync(OperationResult<UserDto>.Success(existingUser));

            // Act
            var result = await controller.RequestAccess();

            // Assert
            var actionResult = Assert.IsAssignableFrom<ActionResult>(result);
            // Controller returns result from base.Update, which is likely OkObjectResult
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var userDto = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(email, userDto.Email);
        }

        [Fact]
        public async Task RequestAccess_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var email = _faker.Internet.Email();

            var claims = new List<Claim>
            {
                new Claim(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()),
                new Claim(ClaimTypes.Email, email),
            };

            var controller = CreateControllerWithContext(claims);

            // Mock base.GetById to return null
            _mockUserService
                .Setup(s => s.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((UserDto)null);

            // Act
            var result = await controller.RequestAccess();

            // Assert
            var actionResult = Assert.IsAssignableFrom<ActionResult>(result);
            Assert.IsType<NotFoundResult>(actionResult);
        }

        [Fact]
        public async Task RequestAccess_ReturnsBadRequest_WhenEmailIsInvalid()
        {
            // Arrange
            var invalidEmail = "not-an-email";

            var claims = new List<Claim>
            {
                new Claim(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()),
                new Claim(ClaimTypes.Email, invalidEmail),
            };

            var controller = CreateControllerWithContext(claims);

            var existingUser = new UserDto { Email = "old@email.com" };

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UserDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<UserDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockUserService
                .Setup(s => s.ValidateAsync(It.IsAny<UserDto>(), It.IsAny<bool>()))
                .ReturnsAsync(OperationResult<UserDto>.Failure("Invalid email format."));

            _mockUserService
                .Setup(s => s.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUser);

            // Act
            var result = await controller.RequestAccess();

            // Assert
            var actionResult = Assert.IsAssignableFrom<ActionResult>(result);
            Assert.IsType<BadRequestObjectResult>(actionResult);
        }

        [Fact]
        public async Task RequestAccess_UpdatesUser_WhenEmailIsUnchanged()
        {
            // Arrange
            var email = _faker.Internet.Email();

            var claims = new List<Claim>
            {
                new Claim(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()),
                new Claim(ClaimTypes.Email, email),
            };

            var controller = CreateControllerWithContext(claims);

            var existingUser = new UserDto { Email = email };

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<UserDto>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<UserDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockUserService
                .Setup(s => s.ValidateAsync(It.IsAny<UserDto>(), It.IsAny<bool>()))
                .ReturnsAsync(OperationResult<UserDto>.Success(new UserDto { Email = email }));

            _mockUserService
                .Setup(s => s.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(existingUser);

            _mockUserService
                .Setup(s => s.UpdateAsync(It.IsAny<UserDto>()))
                .ReturnsAsync(OperationResult<UserDto>.Success(existingUser));

            // Act
            var result = await controller.RequestAccess();

            // Assert
            var actionResult = Assert.IsAssignableFrom<ActionResult>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var userDto = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(email, userDto.Email);

            // Ensure UpdateAsync was called
            _mockUserService.Verify(s => s.UpdateAsync(It.IsAny<UserDto>()), Times.Once);
        }

        [Fact]
        public async Task RequestAccess_ReturnsBadRequest_WhenUserIdNull()
        {
            // Arrange
            var claims = new List<Claim> { };

            var controller = CreateControllerWithContext(claims);

            // Act
            var result = await controller.RequestAccess();

            // Assert
            var actionResult = Assert.IsAssignableFrom<ActionResult>(result);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Equal("Invalid user. Please contact the JASPER admin.", badRequest.Value);
        }
    }
}
using System.Collections.Generic;
using System.IO;
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
using Scv.Api.Models;
using Scv.Api.Services;
using Scv.Api.Validators;
using Scv.Core.Helpers;
using Scv.Core.Infrastructure;
using Scv.Models.AccessControlManagement;
using Xunit;

namespace tests.api.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IValidator<UserDto>> _mockValidator;
    private readonly IValidator<ReleaseNotesViewedRequestDto> _releaseNotesViewedRequestValidator;
    private readonly Mock<IValidator<FileUploadRequest>> _mockFileUploadRequestValidator;
    private readonly Mock<IAntiVirusService> _mockAntiVirusService;
    private readonly Mock<ILogger<UsersController>> _mockLogger;
    private readonly Faker _faker;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockValidator = new Mock<IValidator<UserDto>>();
        _releaseNotesViewedRequestValidator = new ReleaseNotesViewedRequestDtoValidator();
        _mockFileUploadRequestValidator = new Mock<IValidator<FileUploadRequest>>();
        _mockAntiVirusService = new Mock<IAntiVirusService>();
        _mockLogger = new Mock<ILogger<UsersController>>();
        _faker = new Faker();
    }

    private UsersController CreateControllerWithContext(IEnumerable<Claim> claims)
    {
        var controller = new UsersController(
            _mockUserService.Object,
            _mockValidator.Object,
            _releaseNotesViewedRequestValidator,
            _mockFileUploadRequestValidator.Object,
            _mockLogger.Object,
            _mockAntiVirusService.Object
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
            new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()),
            new(ClaimTypes.Email, email),
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
        var actionResult = Assert.IsType<ActionResult>(result, exactMatch: false);
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
            new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()),
            new(ClaimTypes.Email, email),
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
        var actionResult = Assert.IsType<ActionResult>(result, exactMatch: false);
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
            new (CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()),
            new (ClaimTypes.Email, email),
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
        var actionResult = Assert.IsType<ActionResult>(result, exactMatch: false);
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
            new (CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()),
            new (ClaimTypes.Email, email),
        };

        var controller = CreateControllerWithContext(claims);

        // Mock base.GetById to return null
        _mockUserService
            .Setup(s => s.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((UserDto)null);

        // Act
        var result = await controller.RequestAccess();

        // Assert
        var actionResult = Assert.IsType<ActionResult>(result, exactMatch: false);
        Assert.IsType<NotFoundResult>(actionResult);
    }

    [Fact]
    public async Task RequestAccess_ReturnsBadRequest_WhenEmailIsInvalid()
    {
        // Arrange
        var invalidEmail = "not-an-email";

        var claims = new List<Claim>
        {
            new (CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()),
            new (ClaimTypes.Email, invalidEmail),
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
        var actionResult = Assert.IsType<ActionResult>(result, exactMatch: false);
        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public async Task RequestAccess_UpdatesUser_WhenEmailIsUnchanged()
    {
        // Arrange
        var email = _faker.Internet.Email();

        var claims = new List<Claim>
        {
            new (CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()),
            new (ClaimTypes.Email, email),
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
        var actionResult = Assert.IsType<ActionResult>(result, exactMatch: false);
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
        var actionResult = Assert.IsType<ActionResult>(result, exactMatch: false);
        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal("Invalid user. Please contact the JASPER admin.", badRequest.Value);
    }

    [Fact]
    public async Task MarkReleaseNotesViewed_ReturnsNoContent_WhenUpdateSucceeds()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId().ToString();
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, userId) };
        var controller = CreateControllerWithContext(claims);

        _mockUserService
            .Setup(s => s.MarkReleaseNotesViewedAsync(userId, It.IsAny<string>(), It.IsAny<System.DateTime>()))
            .ReturnsAsync(OperationResult<UserDto>.Success(new UserDto()));

        var request = new ReleaseNotesViewedRequestDto { Version = "1.0.0" };

        // Act
        var result = await controller.MarkReleaseNotesViewed(request);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockUserService.Verify(
            s => s.MarkReleaseNotesViewedAsync(userId, request.Version, It.IsAny<System.DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkReleaseNotesViewed_ReturnsBadRequest_WhenUserIdMissing()
    {
        // Arrange
        var claims = new List<Claim> { };
        var controller = CreateControllerWithContext(claims);
        var request = new ReleaseNotesViewedRequestDto { Version = "1.0.0" };

        // Act
        var result = await controller.MarkReleaseNotesViewed(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid user. Please contact the JASPER admin.", badRequest.Value);
    }

    [Fact]
    public async Task MarkReleaseNotesViewed_ReturnsBadRequest_WhenVersionMissing()
    {
        // Arrange
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()) };
        var controller = CreateControllerWithContext(claims);

        // Act
        var result = await controller.MarkReleaseNotesViewed(new ReleaseNotesViewedRequestDto { Version = "" });

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Version is required.", badRequest.Value);
    }

    [Fact]
    public async Task MarkReleaseNotesViewed_ReturnsNotFound_WhenUserNotFound()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId().ToString();
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, userId) };
        var controller = CreateControllerWithContext(claims);

        _mockUserService
            .Setup(s => s.MarkReleaseNotesViewedAsync(userId, It.IsAny<string>(), It.IsAny<System.DateTime>()))
            .ReturnsAsync(OperationResult<UserDto>.Failure("User not found."));

        // Act
        var result = await controller.MarkReleaseNotesViewed(new ReleaseNotesViewedRequestDto { Version = "1.0.0" });

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found.", notFound.Value);
    }

    [Fact]
    public async Task MarkReleaseNotesViewed_ReturnsBadRequest_WhenServiceFails()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId().ToString();
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, userId) };
        var controller = CreateControllerWithContext(claims);

        _mockUserService
            .Setup(s => s.MarkReleaseNotesViewedAsync(userId, It.IsAny<string>(), It.IsAny<System.DateTime>()))
            .ReturnsAsync(OperationResult<UserDto>.Failure("Error updating release notes."));

        // Act
        var result = await controller.MarkReleaseNotesViewed(new ReleaseNotesViewedRequestDto { Version = "1.0.0" });

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    private static IFormFile CreateFile(string fileName = "signature.png", string contentType = "image/png", long length = 1024)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length);
        file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((stream, _) => stream.WriteAsync(new byte[checked((int)length)], 0, checked((int)length)));
        return file.Object;
    }

    [Fact]
    public async Task UploadSignature_ReturnsOk_WhenFileIsCleanAndUploadSucceeds()
    {
        // Arrange
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()) };
        var controller = CreateControllerWithContext(claims);
        var userId = ObjectId.GenerateNewId().ToString();
        var request = new FileUploadRequest { File = CreateFile() };

        _mockFileUploadRequestValidator
            .Setup(v => v.ValidateAsync(It.IsAny<FileUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockAntiVirusService
            .Setup(s => s.ScanAsync(It.IsAny<Stream>()))
            .ReturnsAsync((true, "OK"));

        _mockUserService
            .Setup(s => s.UploadSignatureAsync(userId, It.IsAny<byte[]>()))
            .ReturnsAsync(OperationResult.Success());

        // Act
        var result = await controller.UploadSignature(userId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockUserService.Verify(s => s.UploadSignatureAsync(userId, It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public async Task UploadSignature_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()) };
        var controller = CreateControllerWithContext(claims);
        var request = new FileUploadRequest { File = null };

        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("File", "No file uploaded.")
        };

        _mockFileUploadRequestValidator
            .Setup(v => v.ValidateAsync(It.IsAny<FileUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(validationFailures));

        // Act
        var result = await controller.UploadSignature(ObjectId.GenerateNewId().ToString(), request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file uploaded.", badRequest.Value);
        _mockAntiVirusService.Verify(s => s.ScanAsync(It.IsAny<Stream>()), Times.Never);
        _mockUserService.Verify(s => s.UploadSignatureAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task UploadSignature_ReturnsBadRequest_WhenAntiVirusScanFails()
    {
        // Arrange
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()) };
        var controller = CreateControllerWithContext(claims);
        var request = new FileUploadRequest { File = CreateFile() };

        _mockFileUploadRequestValidator
            .Setup(v => v.ValidateAsync(It.IsAny<FileUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockAntiVirusService
            .Setup(s => s.ScanAsync(It.IsAny<Stream>()))
            .ReturnsAsync((false, "Virus found"));

        // Act
        var result = await controller.UploadSignature(ObjectId.GenerateNewId().ToString(), request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The uploaded file failed the antivirus scan.", badRequest.Value);
        _mockUserService.Verify(s => s.UploadSignatureAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task UploadSignature_ReturnsBadRequest_WhenRequestIsNull()
    {
        // Arrange
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()) };
        var controller = CreateControllerWithContext(claims);
        FileUploadRequest request = null;

        // Act
        var result = await controller.UploadSignature(ObjectId.GenerateNewId().ToString(), request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file was uploaded.", badRequest.Value);
        _mockAntiVirusService.Verify(s => s.ScanAsync(It.IsAny<Stream>()), Times.Never);
        _mockUserService.Verify(s => s.UploadSignatureAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task UploadSignature_ReturnsBadRequest_WhenRequestIsNull()
    {
        // Arrange
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()) };
        var controller = CreateControllerWithContext(claims);
        FileUploadRequest request = null;

        // Act
        var result = await controller.UploadSignature(ObjectId.GenerateNewId().ToString(), request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file was uploaded.", badRequest.Value);
        _mockAntiVirusService.Verify(s => s.ScanAsync(It.IsAny<Stream>()), Times.Never);
        _mockUserService.Verify(s => s.UploadSignatureAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task UploadInitials_ReturnsOk_WhenFileIsCleanAndUploadSucceeds()
    {
        // Arrange
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()) };
        var controller = CreateControllerWithContext(claims);
        var userId = ObjectId.GenerateNewId().ToString();
        var request = new FileUploadRequest { File = CreateFile("initials.png") };

        _mockFileUploadRequestValidator
            .Setup(v => v.ValidateAsync(It.IsAny<FileUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockAntiVirusService
            .Setup(s => s.ScanAsync(It.IsAny<Stream>()))
            .ReturnsAsync((true, "OK"));

        _mockUserService
            .Setup(s => s.UploadInitialsAsync(userId, It.IsAny<byte[]>()))
            .ReturnsAsync(OperationResult.Success());

        // Act
        var result = await controller.UploadInitials(userId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockUserService.Verify(s => s.UploadInitialsAsync(userId, It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public async Task UploadInitials_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()) };
        var controller = CreateControllerWithContext(claims);
        var request = new FileUploadRequest { File = null };

        var validationFailures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("File", "No file uploaded.")
        };

        _mockFileUploadRequestValidator
            .Setup(v => v.ValidateAsync(It.IsAny<FileUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(validationFailures));

        // Act
        var result = await controller.UploadInitials(ObjectId.GenerateNewId().ToString(), request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file uploaded.", badRequest.Value);
        _mockAntiVirusService.Verify(s => s.ScanAsync(It.IsAny<Stream>()), Times.Never);
        _mockUserService.Verify(s => s.UploadInitialsAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task UploadInitials_ReturnsBadRequest_WhenRequestIsNull()
    {
        // Arrange
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()) };
        var controller = CreateControllerWithContext(claims);
        FileUploadRequest request = null;

        // Act
        var result = await controller.UploadInitials(ObjectId.GenerateNewId().ToString(), request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file was uploaded.", badRequest.Value);
        _mockAntiVirusService.Verify(s => s.ScanAsync(It.IsAny<Stream>()), Times.Never);
        _mockUserService.Verify(s => s.UploadInitialsAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task UploadInitials_ReturnsBadRequest_WhenRequestIsNull()
    {
        // Arrange
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()) };
        var controller = CreateControllerWithContext(claims);
        FileUploadRequest request = null;

        // Act
        var result = await controller.UploadInitials(ObjectId.GenerateNewId().ToString(), request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No file was uploaded.", badRequest.Value);
        _mockAntiVirusService.Verify(s => s.ScanAsync(It.IsAny<Stream>()), Times.Never);
        _mockUserService.Verify(s => s.UploadInitialsAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task UploadInitials_ReturnsBadRequest_WhenAntiVirusScanFails()
    {
        // Arrange
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, ObjectId.GenerateNewId().ToString()) };
        var controller = CreateControllerWithContext(claims);
        var request = new FileUploadRequest { File = CreateFile("initials.png") };

        _mockFileUploadRequestValidator
            .Setup(v => v.ValidateAsync(It.IsAny<FileUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockAntiVirusService
            .Setup(s => s.ScanAsync(It.IsAny<Stream>()))
            .ReturnsAsync((false, "Virus found"));

        // Act
        var result = await controller.UploadInitials(ObjectId.GenerateNewId().ToString(), request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The uploaded file failed the antivirus scan.", badRequest.Value);
        _mockUserService.Verify(s => s.UploadInitialsAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }
}

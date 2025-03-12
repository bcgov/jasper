using System.Threading;
using System.Threading.Tasks;
using Bogus;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Infrastructure;
using Scv.Api.Models.UserManagement;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Controllers;

public class PermissionsControllerTests
{
    private readonly Mock<IPermissionService> _mockPermissionService;
    private readonly Mock<IValidator<PermissionUpdateDto>> _mockValidator;
    private readonly PermissionsController _controller;
    private readonly Faker _faker;

    public PermissionsControllerTests()
    {
        _mockPermissionService = new Mock<IPermissionService>();
        _mockValidator = new Mock<IValidator<PermissionUpdateDto>>();
        _controller = new PermissionsController(_mockPermissionService.Object, _mockValidator.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task GetPermissions_ReturnsOkResult_WhenPermissionsExist()
    {
        _mockPermissionService.Setup(s => s.GetPermissionsAsync()).ReturnsAsync([]);

        var result = await _controller.GetPermissions();

        Assert.IsType<OkObjectResult>(result);
        _mockPermissionService.Verify(p => p.GetPermissionsAsync(), Times.Once());
    }

    [Fact]
    public async Task GetPermissionById_ReturnsNotFoundResult_WhenPermissionDoesNotExist()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        _mockPermissionService.Setup(s => s.GetPermissionByIdAsync(fakeId)).ReturnsAsync((PermissionDto)null);

        var result = await _controller.GetPermissionById(fakeId);

        Assert.IsType<NotFoundResult>(result);
        _mockPermissionService.Verify(p => p.GetPermissionByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task GetPermissionById_ReturnsBadRequest_WhenIdIsInvalid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        _mockPermissionService.Setup(s => s.GetPermissionByIdAsync(fakeId)).ReturnsAsync((PermissionDto)null);

        var result = await _controller.GetPermissionById(fakeId);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockPermissionService.Verify(p => p.GetPermissionByIdAsync(fakeId), Times.Never);
    }

    [Fact]
    public async Task GetPermissionById_ReturnsOkResult_WhenPermissionExists()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var permission = new PermissionDto();
        _mockPermissionService.Setup(s => s.GetPermissionByIdAsync(fakeId)).ReturnsAsync(permission);

        var result = await _controller.GetPermissionById(fakeId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<PermissionDto>(okResult.Value);
        Assert.Equal(permission, returnValue);
        _mockPermissionService.Verify(p => p.GetPermissionByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task UpdatePermission_ReturnsBadRequest_WhenBasicValidationFails()
    {
        var mockValidationResult = new FluentValidation.Results.ValidationResult(
            [
                new ValidationFailure(_faker.Random.Word(), _faker.Lorem.Paragraph())
            ]);
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<PermissionUpdateDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockValidationResult);

        var payload = new PermissionUpdateDto
        {
            Description = _faker.Lorem.Paragraph(),
            IsActive = _faker.Random.Bool()
        };

        var result = await _controller.UpdatePermission(_faker.Random.AlphaNumeric(10), payload);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<PermissionUpdateDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePermission_ReturnsBadRequest_WhenBusinessRulesValidationFails()
    {
        var mockPayload = new PermissionUpdateDto
        {
            Description = _faker.Lorem.Paragraph(),
            IsActive = _faker.Random.Bool()
        };
        _mockValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<ValidationContext<PermissionUpdateDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockPermissionService
            .Setup(p => p.ValidatePermissionUpdateDtoAsync(It.IsAny<PermissionUpdateDto>()))
            .ReturnsAsync(OperationResult<PermissionUpdateDto>.Failure([_faker.Lorem.Paragraph()]));


        var result = await _controller.UpdatePermission(_faker.Random.AlphaNumeric(10), mockPayload);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<PermissionUpdateDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockPermissionService.Verify(p =>
            p.ValidatePermissionUpdateDtoAsync(It.IsAny<PermissionUpdateDto>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePermission_ReturnsOkResult_WhenPermissionIsUpdated()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var mockPayload = new PermissionUpdateDto
        {
            Id = fakeId,
            Description = _faker.Lorem.Paragraph(),
            IsActive = _faker.Random.Bool()
        };
        _mockValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<ValidationContext<PermissionUpdateDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockPermissionService
            .Setup(p => p.ValidatePermissionUpdateDtoAsync(It.IsAny<PermissionUpdateDto>()))
            .ReturnsAsync(OperationResult<PermissionUpdateDto>.Success(mockPayload));
        _mockPermissionService
            .Setup(s => s.UpdatePermissionAsync(fakeId, mockPayload))
            .ReturnsAsync(OperationResult<PermissionDto>.Success(new PermissionDto()));

        var result = await _controller.UpdatePermission(fakeId, mockPayload);

        Assert.IsType<OkObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<PermissionUpdateDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockPermissionService.Verify(p =>
            p.ValidatePermissionUpdateDtoAsync(It.IsAny<PermissionUpdateDto>()),
            Times.Once);
        _mockPermissionService.Verify(p =>
            p.UpdatePermissionAsync(fakeId, It.IsAny<PermissionUpdateDto>()),
            Times.Once);
    }
}

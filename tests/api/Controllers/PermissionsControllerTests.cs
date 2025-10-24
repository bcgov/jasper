using System.Threading;
using System.Threading.Tasks;
using Bogus;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Infrastructure;
using Scv.Models.AccessControlManagement;
using Scv.Api.Services;
using Xunit;
using Scv.Core.Services;
using Scv.Core.Infrastructure;

namespace tests.api.Controllers;

public class PermissionsControllerTests
{
    private readonly Mock<ICrudService<PermissionDto>> _mockPermissionService;
    private readonly Mock<IValidator<PermissionDto>> _mockValidator;
    private readonly PermissionsController _controller;
    private readonly Faker _faker;

    public PermissionsControllerTests()
    {
        _mockPermissionService = new Mock<ICrudService<PermissionDto>>();
        _mockValidator = new Mock<IValidator<PermissionDto>>();
        _controller = new PermissionsController(_mockPermissionService.Object, _mockValidator.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task GetPermissions_ReturnsOkResult_WhenPermissionsExist()
    {
        _mockPermissionService.Setup(s => s.GetAllAsync()).ReturnsAsync([]);

        var result = await _controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
        _mockPermissionService.Verify(p => p.GetAllAsync(), Times.Once());
    }

    [Fact]
    public async Task GetPermissionById_ReturnsNotFoundResult_WhenPermissionDoesNotExist()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        _mockPermissionService.Setup(s => s.GetByIdAsync(fakeId)).ReturnsAsync((PermissionDto)null);

        var result = await _controller.GetById(fakeId);

        Assert.IsType<NotFoundResult>(result);
        _mockPermissionService.Verify(p => p.GetByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task GetPermissionById_ReturnsBadRequest_WhenIdIsInvalid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        _mockPermissionService.Setup(s => s.GetByIdAsync(fakeId)).ReturnsAsync((PermissionDto)null);

        var result = await _controller.GetById(fakeId);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockPermissionService.Verify(p => p.GetByIdAsync(fakeId), Times.Never);
    }

    [Fact]
    public async Task GetPermissionById_ReturnsOkResult_WhenPermissionExists()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var permission = new PermissionDto();
        _mockPermissionService.Setup(s => s.GetByIdAsync(fakeId)).ReturnsAsync(permission);

        var result = await _controller.GetById(fakeId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<PermissionDto>(okResult.Value);
        Assert.Equal(permission, returnValue);
        _mockPermissionService.Verify(p => p.GetByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task UpdatePermission_ReturnsBadRequest_WhenBasicValidationFails()
    {
        var mockValidationResult = new FluentValidation.Results.ValidationResult(
            [
                new ValidationFailure(_faker.Random.Word(), _faker.Lorem.Paragraph())
            ]);
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<PermissionDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockValidationResult);

        var payload = new PermissionDto
        {
            Description = _faker.Lorem.Paragraph(),
            IsActive = _faker.Random.Bool()
        };

        var result = await _controller.Update(_faker.Random.AlphaNumeric(10), payload);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<PermissionDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePermission_ReturnsBadRequest_WhenBusinessRulesValidationFails()
    {
        var mockPayload = new PermissionDto
        {
            Description = _faker.Lorem.Paragraph(),
            IsActive = _faker.Random.Bool()
        };
        _mockValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<ValidationContext<PermissionDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockPermissionService
            .Setup(p => p.ValidateAsync(It.IsAny<PermissionDto>(), It.IsAny<bool>()))
            .ReturnsAsync(OperationResult<PermissionDto>.Failure([_faker.Lorem.Paragraph()]));


        var result = await _controller.Update(_faker.Random.AlphaNumeric(10), mockPayload);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<PermissionDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockPermissionService.Verify(p =>
            p.ValidateAsync(It.IsAny<PermissionDto>(), It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePermission_ReturnsOkResult_WhenPermissionIsUpdated()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var mockPayload = new PermissionDto
        {
            Id = fakeId,
            Description = _faker.Lorem.Paragraph(),
            IsActive = _faker.Random.Bool()
        };
        _mockValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<ValidationContext<PermissionDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockPermissionService
            .Setup(p => p.ValidateAsync(It.IsAny<PermissionDto>(), It.IsAny<bool>()))
            .ReturnsAsync(OperationResult<PermissionDto>.Success(mockPayload));
        _mockPermissionService
            .Setup(s => s.UpdateAsync(mockPayload))
            .ReturnsAsync(OperationResult<PermissionDto>.Success(new PermissionDto()));

        var result = await _controller.Update(fakeId, mockPayload);

        Assert.IsType<OkObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<PermissionDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockPermissionService.Verify(p =>
            p.ValidateAsync(It.IsAny<PermissionDto>(), It.IsAny<bool>()),
            Times.Once);
        _mockPermissionService.Verify(p =>
            p.UpdateAsync(It.IsAny<PermissionDto>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePermission_ShouldReturnStatus405MethodNotAllowed()
    {
        var result = await _controller.Create(new PermissionDto());

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status405MethodNotAllowed, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task DeletePermission_ShouldReturnStatus405MethodNotAllowed()
    {
        var result = await _controller.Delete(string.Empty);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status405MethodNotAllowed, statusCodeResult.StatusCode);
    }
}

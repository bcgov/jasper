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
using Scv.Models.AccessControlManagement;
using Scv.Api.Services;
using Xunit;
using Scv.Core.Services;
using Scv.Core.Infrastructure;

namespace tests.api.Controllers;
public class RolesControllerTests
{
    private readonly Mock<ICrudService<RoleDto>> _mockRoleService;
    private readonly Mock<IValidator<RoleDto>> _mockValidator;
    private readonly RolesController _controller;
    private readonly Faker _faker;

    public RolesControllerTests()
    {
        _mockRoleService = new Mock<ICrudService<RoleDto>>();
        _mockValidator = new Mock<IValidator<RoleDto>>();
        _controller = new RolesController(
            _mockRoleService.Object,
            _mockValidator.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task GetRoles_ReturnsOkResult_WhenRolesExist()
    {
        _mockRoleService.Setup(s => s.GetAllAsync()).ReturnsAsync([]);

        var result = await _controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
        _mockRoleService.Verify(p => p.GetAllAsync(), Times.Once());
    }


    [Fact]
    public async Task GetRoleById_ReturnsNotFoundResult_WhenRoleDoesNotExist()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        _mockRoleService.Setup(s => s.GetByIdAsync(fakeId)).ReturnsAsync((RoleDto)null);

        var result = await _controller.GetById(fakeId);

        Assert.IsType<NotFoundResult>(result);
        _mockRoleService.Verify(p => p.GetByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task GetRoleById_ReturnsBadRequest_WhenIdIsInvalid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        _mockRoleService.Setup(s => s.GetByIdAsync(fakeId)).ReturnsAsync((RoleDto)null);

        var result = await _controller.GetById(fakeId);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockRoleService.Verify(p => p.GetByIdAsync(fakeId), Times.Never);
    }

    [Fact]
    public async Task GetRoleById_ReturnsOkResult_WhenRoleExists()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var role = new RoleDto();
        _mockRoleService.Setup(s => s.GetByIdAsync(fakeId)).ReturnsAsync(role);

        var result = await _controller.GetById(fakeId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<RoleDto>(okResult.Value);
        Assert.Equal(role, returnValue);
        _mockRoleService.Verify(p => p.GetByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task CreateRole_ReturnsBadRequest_WhenBasicValidationFails()
    {
        var mockValidationResult = new FluentValidation.Results.ValidationResult(
            [
                new ValidationFailure(_faker.Random.Word(), _faker.Lorem.Paragraph())
            ]);
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<RoleDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockValidationResult);

        var result = await _controller.Create(new RoleDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<RoleDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRole_ReturnsBadRequest_WhenBusinessRulesValidationFails()
    {
        _mockValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<RoleDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockRoleService
            .Setup(p => p.ValidateAsync(It.IsAny<RoleDto>(), It.IsAny<bool>()))
            .ReturnsAsync(OperationResult<RoleDto>.Failure([_faker.Lorem.Paragraph()]));


        var result = await _controller.Create(new RoleDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<RoleDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockRoleService.Verify(p =>
            p.ValidateAsync(It.IsAny<RoleDto>(), It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRole_ReturnsOkResult_WhenRoleIsCreated()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var mockPayload = new RoleDto
        {
            Name = _faker.Random.AlphaNumeric(10),
            Description = _faker.Lorem.Paragraph()
        };
        _mockValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<RoleDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockRoleService
            .Setup(p => p.ValidateAsync(It.IsAny<RoleDto>(), It.IsAny<bool>()))
            .ReturnsAsync(OperationResult<RoleDto>.Success(mockPayload));
        _mockRoleService
            .Setup(s => s.AddAsync(mockPayload))
            .ReturnsAsync(OperationResult<RoleDto>.Success(new RoleDto()));

        var result = await _controller.Create(mockPayload);

        Assert.IsType<CreatedAtActionResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<RoleDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockRoleService.Verify(p =>
            p.ValidateAsync(It.IsAny<RoleDto>(), It.IsAny<bool>()),
            Times.Once);
        _mockRoleService.Verify(p =>
            p.AddAsync(It.IsAny<RoleDto>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRole_ReturnsBadRequest_WhenBasicValidationFails()
    {
        var mockValidationResult = new FluentValidation.Results.ValidationResult(
            [
                new ValidationFailure(_faker.Random.Word(), _faker.Lorem.Paragraph())
            ]);
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<RoleDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockValidationResult);

        var result = await _controller.Update(_faker.Random.AlphaNumeric(10), new RoleDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<RoleDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRole_ReturnsBadRequest_WhenBusinessRulesValidationFails()
    {
        _mockValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<ValidationContext<RoleDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockRoleService
            .Setup(p => p.ValidateAsync(It.IsAny<RoleDto>(), It.IsAny<bool>()))
            .ReturnsAsync(OperationResult<RoleDto>.Failure([_faker.Lorem.Paragraph()]));


        var result = await _controller.Update(_faker.Random.AlphaNumeric(10), new RoleDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<RoleDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockRoleService.Verify(p =>
            p.ValidateAsync(It.IsAny<RoleDto>(), It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRole_ReturnsOkResult_WhenRoleIsUpdated()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var mockPayload = new RoleDto
        {
            Id = fakeId,
            Name = _faker.Random.AlphaNumeric(10),
            Description = _faker.Lorem.Paragraph()
        };
        _mockValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<ValidationContext<RoleDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockRoleService
            .Setup(p => p.ValidateAsync(It.IsAny<RoleDto>(), It.IsAny<bool>()))
            .ReturnsAsync(OperationResult<RoleDto>.Success(mockPayload));
        _mockRoleService
            .Setup(s => s.UpdateAsync(mockPayload))
            .ReturnsAsync(OperationResult<RoleDto>.Success(new RoleDto()));

        var result = await _controller.Update(fakeId, mockPayload);

        Assert.IsType<OkObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<RoleDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockRoleService.Verify(p =>
            p.ValidateAsync(It.IsAny<RoleDto>(), It.IsAny<bool>()),
            Times.Once);
        _mockRoleService.Verify(p =>
            p.UpdateAsync(It.IsAny<RoleDto>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteRole_ReturnsNotFoundResult_WhenRoleDoesNotExist()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        _mockRoleService
            .Setup(s => s.DeleteAsync(fakeId))
            .ReturnsAsync(OperationResult.Failure([_faker.Lorem.Paragraph()]));

        var result = await _controller.GetById(fakeId);

        Assert.IsType<NotFoundResult>(result);
        _mockRoleService.Verify(p => p.GetByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task DeleteRole_ReturnsBadRequest_WhenIdIsInvalid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        _mockRoleService
            .Setup(s => s.DeleteAsync(fakeId))
            .ReturnsAsync(OperationResult.Failure([_faker.Lorem.Paragraph()]));

        var result = await _controller.Delete(fakeId);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockRoleService.Verify(p => p.DeleteAsync(fakeId), Times.Never);
    }

    [Fact]
    public async Task DeleteRole_ReturnsOkResult_WhenRoleExists()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var role = new RoleDto();
        _mockRoleService.Setup(s => s.DeleteAsync(fakeId)).ReturnsAsync(OperationResult.Success());

        var result = await _controller.Delete(fakeId);

        Assert.IsType<NoContentResult>(result);
        _mockRoleService.Verify(p => p.DeleteAsync(fakeId), Times.Once());
    }
}

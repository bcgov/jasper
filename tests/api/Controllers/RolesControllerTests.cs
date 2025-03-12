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
public class RolesControllerTests
{
    private readonly Mock<IRoleService> _mockRoleService;
    private readonly Mock<IValidator<RoleCreateDto>> _mockCreateValidator;
    private readonly Mock<IValidator<RoleUpdateDto>> _mockUpdateValidator;
    private readonly RolesController _controller;
    private readonly Faker _faker;

    public RolesControllerTests()
    {
        _mockRoleService = new Mock<IRoleService>();
        _mockCreateValidator = new Mock<IValidator<RoleCreateDto>>();
        _mockUpdateValidator = new Mock<IValidator<RoleUpdateDto>>();
        _controller = new RolesController(
            _mockRoleService.Object,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task GetRoles_ReturnsOkResult_WhenRolesExist()
    {
        _mockRoleService.Setup(s => s.GetRolesAsync()).ReturnsAsync([]);

        var result = await _controller.GetRoles();

        Assert.IsType<OkObjectResult>(result);
        _mockRoleService.Verify(p => p.GetRolesAsync(), Times.Once());
    }


    [Fact]
    public async Task GetRoleById_ReturnsNotFoundResult_WhenRoleDoesNotExist()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        _mockRoleService.Setup(s => s.GetRoleByIdAsync(fakeId)).ReturnsAsync((RoleDto)null);

        var result = await _controller.GetRoleById(fakeId);

        Assert.IsType<NotFoundResult>(result);
        _mockRoleService.Verify(p => p.GetRoleByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task GetRoleById_ReturnsBadRequest_WhenIdIsInvalid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        _mockRoleService.Setup(s => s.GetRoleByIdAsync(fakeId)).ReturnsAsync((RoleDto)null);

        var result = await _controller.GetRoleById(fakeId);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockRoleService.Verify(p => p.GetRoleByIdAsync(fakeId), Times.Never);
    }

    [Fact]
    public async Task GetRoleById_ReturnsOkResult_WhenRoleExists()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var role = new RoleDto();
        _mockRoleService.Setup(s => s.GetRoleByIdAsync(fakeId)).ReturnsAsync(role);

        var result = await _controller.GetRoleById(fakeId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<RoleDto>(okResult.Value);
        Assert.Equal(role, returnValue);
        _mockRoleService.Verify(p => p.GetRoleByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task CreateRole_ReturnsBadRequest_WhenBasicValidationFails()
    {
        var mockValidationResult = new FluentValidation.Results.ValidationResult(
            [
                new ValidationFailure(_faker.Random.Word(), _faker.Lorem.Paragraph())
            ]);
        _mockCreateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<RoleCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockValidationResult);

        var result = await _controller.CreateRole(new RoleCreateDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockCreateValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<RoleCreateDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRole_ReturnsBadRequest_WhenBusinessRulesValidationFails()
    {
        _mockCreateValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<RoleCreateDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockRoleService
            .Setup(p => p.ValidateRoleCreateDtoAsync(It.IsAny<RoleCreateDto>()))
            .ReturnsAsync(OperationResult<RoleCreateDto>.Failure([_faker.Lorem.Paragraph()]));


        var result = await _controller.CreateRole(new RoleCreateDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockCreateValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<RoleCreateDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockRoleService.Verify(p =>
            p.ValidateRoleCreateDtoAsync(It.IsAny<RoleCreateDto>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRole_ReturnsOkResult_WhenRoleIsCreated()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var mockPayload = new RoleCreateDto
        {
            Name = _faker.Random.AlphaNumeric(10),
            Description = _faker.Lorem.Paragraph()
        };
        _mockCreateValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<RoleCreateDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockRoleService
            .Setup(p => p.ValidateRoleCreateDtoAsync(It.IsAny<RoleCreateDto>()))
            .ReturnsAsync(OperationResult<RoleCreateDto>.Success(mockPayload));
        _mockRoleService
            .Setup(s => s.CreateRoleAsync(mockPayload))
            .ReturnsAsync(OperationResult<RoleDto>.Success(new RoleDto()));

        var result = await _controller.CreateRole(mockPayload);

        Assert.IsType<CreatedAtActionResult>(result);
        _mockCreateValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<RoleCreateDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockRoleService.Verify(p =>
            p.ValidateRoleCreateDtoAsync(It.IsAny<RoleCreateDto>()),
            Times.Once);
        _mockRoleService.Verify(p =>
            p.CreateRoleAsync(It.IsAny<RoleCreateDto>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRole_ReturnsBadRequest_WhenBasicValidationFails()
    {
        var mockValidationResult = new FluentValidation.Results.ValidationResult(
            [
                new ValidationFailure(_faker.Random.Word(), _faker.Lorem.Paragraph())
            ]);
        _mockUpdateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<RoleUpdateDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockValidationResult);

        var result = await _controller.UpdateRole(_faker.Random.AlphaNumeric(10), new RoleUpdateDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockUpdateValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<RoleUpdateDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRole_ReturnsBadRequest_WhenBusinessRulesValidationFails()
    {
        _mockUpdateValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<ValidationContext<RoleUpdateDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockRoleService
            .Setup(p => p.ValidateRoleUpdateDtoAsync(It.IsAny<RoleUpdateDto>()))
            .ReturnsAsync(OperationResult<RoleUpdateDto>.Failure([_faker.Lorem.Paragraph()]));


        var result = await _controller.UpdateRole(_faker.Random.AlphaNumeric(10), new RoleUpdateDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockUpdateValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<RoleUpdateDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockRoleService.Verify(p =>
            p.ValidateRoleUpdateDtoAsync(It.IsAny<RoleUpdateDto>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRole_ReturnsOkResult_WhenRoleIsUpdated()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var mockPayload = new RoleUpdateDto
        {
            Id = fakeId,
            Name = _faker.Random.AlphaNumeric(10),
            Description = _faker.Lorem.Paragraph()
        };
        _mockUpdateValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<ValidationContext<RoleUpdateDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockRoleService
            .Setup(p => p.ValidateRoleUpdateDtoAsync(It.IsAny<RoleUpdateDto>()))
            .ReturnsAsync(OperationResult<RoleUpdateDto>.Success(mockPayload));
        _mockRoleService
            .Setup(s => s.UpdateRoleAsync(fakeId, mockPayload))
            .ReturnsAsync(OperationResult<RoleDto>.Success(new RoleDto()));

        var result = await _controller.UpdateRole(fakeId, mockPayload);

        Assert.IsType<OkObjectResult>(result);
        _mockUpdateValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<RoleUpdateDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockRoleService.Verify(p =>
            p.ValidateRoleUpdateDtoAsync(It.IsAny<RoleUpdateDto>()),
            Times.Once);
        _mockRoleService.Verify(p =>
            p.UpdateRoleAsync(fakeId, It.IsAny<RoleUpdateDto>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteRole_ReturnsNotFoundResult_WhenRoleDoesNotExist()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        _mockRoleService
            .Setup(s => s.DeleteRoleAsync(fakeId))
            .ReturnsAsync(OperationResult.Failure([_faker.Lorem.Paragraph()]));

        var result = await _controller.GetRoleById(fakeId);

        Assert.IsType<NotFoundResult>(result);
        _mockRoleService.Verify(p => p.GetRoleByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task DeleteRole_ReturnsBadRequest_WhenIdIsInvalid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        _mockRoleService
            .Setup(s => s.DeleteRoleAsync(fakeId))
            .ReturnsAsync(OperationResult.Failure([_faker.Lorem.Paragraph()]));

        var result = await _controller.DeleteRole(fakeId);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockRoleService.Verify(p => p.DeleteRoleAsync(fakeId), Times.Never);
    }

    [Fact]
    public async Task DeleteRole_ReturnsOkResult_WhenRoleExists()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var role = new RoleDto();
        _mockRoleService.Setup(s => s.DeleteRoleAsync(fakeId)).ReturnsAsync(OperationResult.Success());

        var result = await _controller.DeleteRole(fakeId);

        Assert.IsType<NoContentResult>(result);
        _mockRoleService.Verify(p => p.DeleteRoleAsync(fakeId), Times.Once());
    }
}

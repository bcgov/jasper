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
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Controllers;
public class GroupsControllerTests
{
    private readonly Mock<IAccessControlManagementService<GroupDto>> _mockGroupService;
    private readonly Mock<IValidator<GroupDto>> _mockValidator;
    private readonly GroupsController _controller;
    private readonly Faker _faker;

    public GroupsControllerTests()
    {
        _mockGroupService = new Mock<IAccessControlManagementService<GroupDto>>();
        _mockValidator = new Mock<IValidator<GroupDto>>();
        _controller = new GroupsController(
            _mockGroupService.Object,
            _mockValidator.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task GetGroups_ReturnsOkResult_WhenGroupsExist()
    {
        _mockGroupService.Setup(s => s.GetAllAsync()).ReturnsAsync([]);

        var result = await _controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
        _mockGroupService.Verify(p => p.GetAllAsync(), Times.Once());
    }


    [Fact]
    public async Task GetGroupById_ReturnsNotFoundResult_WhenGroupDoesNotExist()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        _mockGroupService.Setup(s => s.GetByIdAsync(fakeId)).ReturnsAsync((GroupDto)null);

        var result = await _controller.GetById(fakeId);

        Assert.IsType<NotFoundResult>(result);
        _mockGroupService.Verify(p => p.GetByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task GetGroupById_ReturnsBadRequest_WhenIdIsInvalid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        _mockGroupService.Setup(s => s.GetByIdAsync(fakeId)).ReturnsAsync((GroupDto)null);

        var result = await _controller.GetById(fakeId);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockGroupService.Verify(p => p.GetByIdAsync(fakeId), Times.Never);
    }

    [Fact]
    public async Task GetGroupById_ReturnsOkResult_WhenGroupExists()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var group = new GroupDto();
        _mockGroupService.Setup(s => s.GetByIdAsync(fakeId)).ReturnsAsync(group);

        var result = await _controller.GetById(fakeId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<GroupDto>(okResult.Value);
        Assert.Equal(group, returnValue);
        _mockGroupService.Verify(p => p.GetByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task CreateGroup_ReturnsBadRequest_WhenBasicValidationFails()
    {
        var mockValidationResult = new FluentValidation.Results.ValidationResult(
            [
                new ValidationFailure(_faker.Random.Word(), _faker.Lorem.Paragraph())
            ]);
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GroupDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockValidationResult);

        var result = await _controller.Create(new GroupDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<GroupDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateGroup_ReturnsBadRequest_WhenBusinessRulesValidationFails()
    {
        _mockValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<GroupDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockGroupService
            .Setup(p => p.ValidateAsync(It.IsAny<GroupDto>(), It.IsAny<bool>()))
            .ReturnsAsync(OperationResult<GroupDto>.Failure([_faker.Lorem.Paragraph()]));


        var result = await _controller.Create(new GroupDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<GroupDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGroupService.Verify(p =>
            p.ValidateAsync(It.IsAny<GroupDto>(), It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateGroup_ReturnsOkResult_WhenGroupIsCreated()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var mockPayload = new GroupDto
        {
            Name = _faker.Random.AlphaNumeric(10),
            Description = _faker.Lorem.Paragraph()
        };
        _mockValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<GroupDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockGroupService
            .Setup(p => p.ValidateAsync(It.IsAny<GroupDto>(), It.IsAny<bool>()))
            .ReturnsAsync(OperationResult<GroupDto>.Success(mockPayload));
        _mockGroupService
            .Setup(s => s.AddAsync(mockPayload))
            .ReturnsAsync(OperationResult<GroupDto>.Success(new GroupDto()));

        var result = await _controller.Create(mockPayload);

        Assert.IsType<CreatedAtActionResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<GroupDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGroupService.Verify(p =>
            p.ValidateAsync(It.IsAny<GroupDto>(), It.IsAny<bool>()),
            Times.Once);
        _mockGroupService.Verify(p =>
            p.AddAsync(It.IsAny<GroupDto>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateGroup_ReturnsBadRequest_WhenBasicValidationFails()
    {
        var mockValidationResult = new FluentValidation.Results.ValidationResult(
            [
                new ValidationFailure(_faker.Random.Word(), _faker.Lorem.Paragraph())
            ]);
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<GroupDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockValidationResult);

        var result = await _controller.Update(_faker.Random.AlphaNumeric(10), new GroupDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<GroupDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateGroup_ReturnsBadRequest_WhenBusinessRulesValidationFails()
    {
        _mockValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<ValidationContext<GroupDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockGroupService
            .Setup(p => p.ValidateAsync(It.IsAny<GroupDto>(), It.IsAny<bool>()))
            .ReturnsAsync(OperationResult<GroupDto>.Failure([_faker.Lorem.Paragraph()]));


        var result = await _controller.Update(_faker.Random.AlphaNumeric(10), new GroupDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<GroupDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGroupService.Verify(p =>
            p.ValidateAsync(It.IsAny<GroupDto>(), It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateGroup_ReturnsOkResult_WhenGroupIsUpdated()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var mockPayload = new GroupDto
        {
            Id = fakeId,
            Name = _faker.Random.AlphaNumeric(10),
            Description = _faker.Lorem.Paragraph()
        };
        _mockValidator
            .Setup(v =>
                v.ValidateAsync(It.IsAny<ValidationContext<GroupDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockGroupService
            .Setup(p => p.ValidateAsync(It.IsAny<GroupDto>(), It.IsAny<bool>()))
            .ReturnsAsync(OperationResult<GroupDto>.Success(mockPayload));
        _mockGroupService
            .Setup(s => s.UpdateAsync(mockPayload))
            .ReturnsAsync(OperationResult<GroupDto>.Success(new GroupDto()));

        var result = await _controller.Update(fakeId, mockPayload);

        Assert.IsType<OkObjectResult>(result);
        _mockValidator.Verify(v =>
            v.ValidateAsync(
                It.IsAny<ValidationContext<GroupDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGroupService.Verify(p =>
            p.ValidateAsync(It.IsAny<GroupDto>(), It.IsAny<bool>()),
            Times.Once);
        _mockGroupService.Verify(p =>
            p.UpdateAsync(It.IsAny<GroupDto>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteGroup_ReturnsNotFoundResult_WhenGroupDoesNotExist()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        _mockGroupService
            .Setup(s => s.DeleteAsync(fakeId))
            .ReturnsAsync(OperationResult.Failure([_faker.Lorem.Paragraph()]));

        var result = await _controller.GetById(fakeId);

        Assert.IsType<NotFoundResult>(result);
        _mockGroupService.Verify(p => p.GetByIdAsync(fakeId), Times.Once());
    }

    [Fact]
    public async Task DeleteGroup_ReturnsBadRequest_WhenIdIsInvalid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        _mockGroupService
            .Setup(s => s.DeleteAsync(fakeId))
            .ReturnsAsync(OperationResult.Failure([_faker.Lorem.Paragraph()]));

        var result = await _controller.Delete(fakeId);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockGroupService.Verify(p => p.DeleteAsync(fakeId), Times.Never);
    }

    [Fact]
    public async Task DeleteGroup_ReturnsOkResult_WhenGroupExists()
    {
        var fakeId = ObjectId.GenerateNewId().ToString();
        var group = new GroupDto();
        _mockGroupService.Setup(s => s.DeleteAsync(fakeId)).ReturnsAsync(OperationResult.Success());

        var result = await _controller.Delete(fakeId);

        Assert.IsType<NoContentResult>(result);
        _mockGroupService.Verify(p => p.DeleteAsync(fakeId), Times.Once());
    }
}

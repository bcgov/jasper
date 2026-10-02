using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bogus;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Services;
using Scv.Core.Infrastructure;
using Scv.Models.UserArtifacts;
using Xunit;

namespace tests.api.Controllers;

public class ArtifactsControllerTests
{
    private readonly Mock<IUserArtifactService> _mockArtifactService;
    private readonly Mock<IValidator<NoteDto>> _mockNoteValidator;
    private readonly Mock<IValidator<AnnotationDto>> _mockAnnotationValidator;
    private readonly ArtifactsController _controller;
    private readonly Faker _faker;

    public ArtifactsControllerTests()
    {
        _mockArtifactService = new Mock<IUserArtifactService>();
        _mockNoteValidator = new Mock<IValidator<NoteDto>>();
        _mockAnnotationValidator = new Mock<IValidator<AnnotationDto>>();
        _controller = new ArtifactsController(
            _mockArtifactService.Object,
            _mockNoteValidator.Object,
            _mockAnnotationValidator.Object);
        _faker = new Faker();

        _mockNoteValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<NoteDto>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockAnnotationValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AnnotationDto>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithNullBody_ReturnsBadRequest()
    {
        var result = await _controller.Create(null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithNoteDto_WhenBasicValidationFails_ReturnsBadRequest()
    {
        _mockNoteValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<NoteDto>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult([new ValidationFailure("Content", "Content is required.")]));

        var result = await _controller.Create(new NoteDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockArtifactService.Verify(s => s.AddAsync(It.IsAny<UserArtifactDto>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithNoteDto_WhenBusinessRuleValidationFails_ReturnsBadRequest()
    {
        var dto = new NoteDto { Content = "hello" };
        _mockArtifactService
            .Setup(s => s.ValidateAsync(dto, false))
            .ReturnsAsync(OperationResult<UserArtifactDto>.Failure("Business rule failed."));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
        _mockArtifactService.Verify(s => s.AddAsync(It.IsAny<UserArtifactDto>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithNoteDto_WhenAddFails_ReturnsBadRequest()
    {
        var dto = new NoteDto { Content = "hello" };
        _mockArtifactService
            .Setup(s => s.ValidateAsync(dto, false))
            .ReturnsAsync(OperationResult<UserArtifactDto>.Success(dto));
        _mockArtifactService
            .Setup(s => s.AddAsync(dto))
            .ReturnsAsync(OperationResult<UserArtifactDto>.Failure("Could not add."));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithNoteDto_WhenSucceeds_ReturnsCreatedAtAction()
    {
        var dto = new NoteDto { Content = "hello" };
        var created = new NoteDto { Id = ObjectId.GenerateNewId().ToString(), Content = "hello" };
        _mockArtifactService
            .Setup(s => s.ValidateAsync(dto, false))
            .ReturnsAsync(OperationResult<UserArtifactDto>.Success(dto));
        _mockArtifactService
            .Setup(s => s.AddAsync(dto))
            .ReturnsAsync(OperationResult<UserArtifactDto>.Success(created));

        var result = await _controller.Create(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(created, createdResult.Value);
    }

    [Fact]
    public async Task Create_WithAnnotationDto_WhenBasicValidationFails_ReturnsBadRequest()
    {
        _mockAnnotationValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AnnotationDto>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult([new ValidationFailure("DocumentId", "Document ID is required.")]));

        var result = await _controller.Create(new AnnotationDto());

        Assert.IsType<BadRequestObjectResult>(result);
        _mockArtifactService.Verify(s => s.AddAsync(It.IsAny<UserArtifactDto>()), Times.Never);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsBadRequest()
    {
        var result = await _controller.GetById(_faker.Random.AlphaNumeric(10));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var id = ObjectId.GenerateNewId().ToString();
        _mockArtifactService.Setup(s => s.GetByIdAsync(id, false)).ReturnsAsync((UserArtifactDto)null);

        var result = await _controller.GetById(id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var id = ObjectId.GenerateNewId().ToString();
        var dto = new NoteDto { Id = id, Content = "hello" };
        _mockArtifactService.Setup(s => s.GetByIdAsync(id, false)).ReturnsAsync(dto);

        var result = await _controller.GetById(id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, okResult.Value);
    }

    [Fact]
    public async Task GetById_WithIncludeDeletedTrue_PassesFlagToService()
    {
        var id = ObjectId.GenerateNewId().ToString();
        var dto = new NoteDto { Id = id, DeletedDate = DateTime.UtcNow };
        _mockArtifactService.Setup(s => s.GetByIdAsync(id, true)).ReturnsAsync(dto);

        var result = await _controller.GetById(id, includeDeleted: true);

        Assert.IsType<OkObjectResult>(result);
        _mockArtifactService.Verify(s => s.GetByIdAsync(id, true), Times.Once);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithNullBody_ReturnsBadRequest()
    {
        var result = await _controller.Update(ObjectId.GenerateNewId().ToString(), null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenUpdateFails_ReturnsBadRequest()
    {
        var id = ObjectId.GenerateNewId().ToString();
        var dto = new NoteDto { Id = id, Content = "updated" };
        _mockArtifactService
            .Setup(s => s.ValidateAsync(dto, true))
            .ReturnsAsync(OperationResult<UserArtifactDto>.Success(dto));
        _mockArtifactService
            .Setup(s => s.UpdateAsync(dto))
            .ReturnsAsync(OperationResult<UserArtifactDto>.Failure("Not authorized."));

        var result = await _controller.Update(id, dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_WhenSucceeds_ReturnsOk()
    {
        var id = ObjectId.GenerateNewId().ToString();
        var dto = new NoteDto { Id = id, Content = "updated" };
        _mockArtifactService
            .Setup(s => s.ValidateAsync(dto, true))
            .ReturnsAsync(OperationResult<UserArtifactDto>.Success(dto));
        _mockArtifactService
            .Setup(s => s.UpdateAsync(dto))
            .ReturnsAsync(OperationResult<UserArtifactDto>.Success(dto));

        var result = await _controller.Update(id, dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, okResult.Value);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsBadRequest()
    {
        var result = await _controller.Delete(_faker.Random.AlphaNumeric(10));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenServiceFails_ReturnsBadRequest()
    {
        var id = ObjectId.GenerateNewId().ToString();
        _mockArtifactService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(OperationResult.Failure("Not found."));

        var result = await _controller.Delete(id);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSucceeds_ReturnsNoContent()
    {
        var id = ObjectId.GenerateNewId().ToString();
        _mockArtifactService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(OperationResult.Success());

        var result = await _controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
        _mockArtifactService.Verify(s => s.DeleteAsync(id), Times.Once);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task Search_WithNullCriteria_DelegatesToServiceWithDefaultCriteria()
    {
        _mockArtifactService
            .Setup(s => s.SearchAsync(It.IsAny<SearchArtifactsCriteria>()))
            .ReturnsAsync([]);

        var result = await _controller.Search(null);

        Assert.IsType<OkObjectResult>(result);
        _mockArtifactService.Verify(s => s.SearchAsync(It.IsAny<SearchArtifactsCriteria>()), Times.Once);
    }

    [Fact]
    public async Task Search_WithCriteria_ReturnsOkWithResults()
    {
        var criteria = new SearchArtifactsCriteria { Keyword = "hello" };
        var results = new List<UserArtifactDto> { new NoteDto { Content = "hello world" } };
        _mockArtifactService.Setup(s => s.SearchAsync(criteria)).ReturnsAsync(results);

        var result = await _controller.Search(criteria);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(results, okResult.Value);
    }

    #endregion
}

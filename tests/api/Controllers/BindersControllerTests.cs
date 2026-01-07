using System.Collections.Generic;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Infrastructure;
using Scv.Models;
using Scv.Api.Services;
using Scv.Db.Contants;
using Xunit;
using Scv.Core.Infrastructure;

namespace tests.api.Controllers;
public class BindersControllerTests
{
    private readonly Faker _faker;
    private readonly Mock<IBinderService> _mockService;
    private readonly BindersController _controller;

    public BindersControllerTests()
    {
        _faker = new Faker();
        _mockService = new Mock<IBinderService>();
        _controller = new BindersController(_mockService.Object);

        var context = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
    }

    [Fact]
    public async Task GetBinders_ReturnsOk_WithResult()
    {
        _controller.Request.QueryString = new QueryString($"?{LabelConstants.PHYSICAL_FILE_ID}={_faker.Random.Number()}");
        _mockService.Setup(s => s.GetByLabels(It.IsAny<Dictionary<string, string>>()))
                    .ReturnsAsync(OperationResult<List<BinderDto>>.Success([]));

        var result = await _controller.GetBinders();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetByLabels(It.IsAny<Dictionary<string, string>>()), Times.Once());
    }

    [Fact]
    public async Task CreateBinder_ReturnsCreated_WhenSuccessful()
    {
        var dto = new BinderDto();
        var expectedResult = OperationResult<BinderDto>.Success(dto);
        _mockService.Setup(s => s.AddAsync(dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CreateBinder(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("GetBinders", created.ActionName);
        Assert.Equal(expectedResult, created.Value);
        _mockService.Verify(s => s.AddAsync(dto), Times.Once());
    }

    [Fact]
    public async Task CreateBinder_ReturnsBadRequest_WhenFailed()
    {
        var errorMessage = _faker.Lorem.Paragraph();
        var dto = new BinderDto();
        var expectedResult = OperationResult<BinderDto>.Failure([errorMessage]);
        _mockService.Setup(s => s.AddAsync(dto)).ReturnsAsync(expectedResult);

        var result = await _controller.CreateBinder(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorProp = badRequest.Value.GetType().GetProperty("error");
        var actualErrors = errorProp.GetValue(badRequest.Value);
        Assert.Equal(expectedResult.Errors, actualErrors);
        _mockService.Verify(s => s.AddAsync(dto), Times.Once());
    }

    [Fact]
    public async Task UpdateBinder_ReturnsOk_WhenSuccessful()
    {
        var dto = new BinderDto();
        var expectedResult = OperationResult<BinderDto>.Success(dto);
        _mockService.Setup(s => s.UpdateAsync(dto)).ReturnsAsync(expectedResult);

        var result = await _controller.UpdateBinder(dto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);

        _mockService.Verify(s => s.UpdateAsync(dto), Times.Once());
    }

    [Fact]
    public async Task UpdateBinder_ReturnsBadRequest_WhenFailed()
    {
        var errorMessage = _faker.Lorem.Paragraph();
        var dto = new BinderDto();
        var expectedResult = OperationResult<BinderDto>.Failure([errorMessage]);
        _mockService.Setup(s => s.UpdateAsync(dto)).ReturnsAsync(expectedResult);

        var result = await _controller.UpdateBinder(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorProp = badRequest.Value.GetType().GetProperty("error");
        var actualErrors = errorProp.GetValue(badRequest.Value);
        Assert.Equal(expectedResult.Errors, actualErrors);
        _mockService.Verify(s => s.UpdateAsync(dto), Times.Once());
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenSuccessful()
    {
        var id = _faker.Random.AlphaNumeric(10);
        var expectedResult = OperationResult.Success;

        _mockService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(expectedResult);

        var result = await _controller.DeleteBinder(id);

        Assert.IsType<NoContentResult>(result);
        _mockService.Verify(s => s.DeleteAsync(id), Times.Once());
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenFailed()
    {
        var errorMessage = _faker.Lorem.Paragraph();
        var id = _faker.Random.AlphaNumeric(10);
        var expectedResult = OperationResult.Failure([errorMessage]);

        _mockService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(expectedResult);

        var result = await _controller.DeleteBinder(id);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errorProp = badRequest.Value.GetType().GetProperty("error");
        var actualErrors = errorProp.GetValue(badRequest.Value);
        Assert.Equal(expectedResult.Errors, actualErrors);
        _mockService.Verify(s => s.DeleteAsync(id), Times.Once());
    }

    [Fact]
    public async Task CreateDocumentBundle_ShouldReturnBadRequest_WhenPayloadIsNull()
    {
        var result = await _controller.CreateDocumentBundle(null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateDocumentBundle_ShouldReturnBadRequest_WhenPayloadIsEmpty()
    {
        var result = await _controller.CreateDocumentBundle([]);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateDocumentBundle_ShouldReturnBadRequest_BinderServiceResultDidNotSucceed()
    {
        _mockService
            .Setup(s => s.CreateDocumentBundle(It.IsAny<List<Dictionary<string, string>>>(), It.IsAny<Dictionary<string, List<string>>>()))
            .ReturnsAsync(OperationResult<DocumentBundleResponse>.Failure(null));

        var result = await _controller.CreateDocumentBundle([[]]);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateDocumentBundle_ShouldReturnOkResult()
    {
        _mockService
            .Setup(s => s.CreateDocumentBundle(It.IsAny<List<Dictionary<string, string>>>(), It.IsAny<Dictionary<string, List<string>>>()))
            .ReturnsAsync(OperationResult<DocumentBundleResponse>.Success(null));

        var result = await _controller.CreateDocumentBundle([[]]);

        var okResult = Assert.IsType<OkObjectResult>(result);

        _mockService.Verify(s => s.CreateDocumentBundle(It.IsAny<List<Dictionary<string, string>>>(), It.IsAny<Dictionary<string, List<string>>>()), Times.Once());
    }
}

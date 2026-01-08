using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bogus;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure;
using Scv.Api.Models.Order;
using Scv.Api.Services;
using Xunit;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace tests.api.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IValidator<OrderDto>> _mockValidator;
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly OrdersController _controller;
    private readonly Faker _faker;
    private readonly int _judgeId;

    public OrdersControllerTests()
    {
        _mockValidator = new Mock<IValidator<OrderDto>>();
        _mockOrderService = new Mock<IOrderService>();
        _faker = new Faker();
        _judgeId = _faker.Random.Int(1, 1000);

        _controller = new OrdersController(_mockValidator.Object, _mockOrderService.Object);

        var claims = new List<Claim>
        {
            new (CustomClaimTypes.JudgeId, _judgeId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region GetMyOrders Tests

    [Fact]
    public async Task GetMyOrders_ReturnsOk_WithFilteredOrders()
    {
        var orders = new List<OrderDto>
        {
            new()
            {
                CourtFile = new CourtFileDto { PhysicalFileId = 123 },
                Referral = new ReferralDto { SentToPartId = _judgeId }
            },
            new()
            {
                CourtFile = new CourtFileDto { PhysicalFileId = 456 },
                Referral = new ReferralDto { SentToPartId = _faker.Random.Int(2000, 3000) }
            },
            new()
            {
                CourtFile = new CourtFileDto { PhysicalFileId = 789 },
                Referral = new ReferralDto { SentToPartId = _judgeId }
            }
        };

        _mockOrderService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(orders);

        var result = await _controller.GetMyOrders();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedOrders = Assert.IsAssignableFrom<IEnumerable<OrderDto>>(okResult.Value);
        Assert.Equal(2, returnedOrders.Count());
        Assert.All(returnedOrders, order => Assert.Equal(_judgeId, order.Referral.SentToPartId));
        _mockOrderService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetMyOrders_ReturnsOk_WithEmptyList_WhenNoOrdersMatch()
    {
        var orders = new List<OrderDto>
        {
            new()
            {
                CourtFile = new CourtFileDto { PhysicalFileId = 123 },
                Referral = new ReferralDto { SentToPartId = _faker.Random.Int(2000, 3000) }
            }
        };

        _mockOrderService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(orders);

        var result = await _controller.GetMyOrders();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedOrders = Assert.IsAssignableFrom<IEnumerable<OrderDto>>(okResult.Value);
        Assert.Empty(returnedOrders);
        _mockOrderService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    #endregion

    #region UpsertOrder Tests

    [Fact]
    public async Task UpsertOrder_ReturnsUnprocessableEntity_WhenBasicValidationFails()
    {
        var orderDto = CreateValidOrderDto();
        var validationFailures = new List<ValidationFailure>
        {
            new("CourtFile", "CourtFile is required.")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockValidator
            .Setup(v => v.ValidateAsync(orderDto, default))
            .ReturnsAsync(validationResult);

        var result = await _controller.UpsertOrder(orderDto);

        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(unprocessableResult.Value);
        Assert.Contains("CourtFile is required.", errors);
        _mockValidator.Verify(v => v.ValidateAsync(orderDto, default), Times.Once);
        _mockOrderService.Verify(s => s.ValidateAsync(It.IsAny<OrderDto>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task UpsertOrder_ReturnsUnprocessableEntity_WhenBusinessValidationFails()
    {
        var orderDto = CreateValidOrderDto();

        _mockValidator
            .Setup(v => v.ValidateAsync(orderDto, default))
            .ReturnsAsync(new ValidationResult());

        _mockOrderService
            .Setup(s => s.ValidateAsync(orderDto, false))
            .ReturnsAsync(OperationResult<OrderDto>.Failure("Criminal file with id: 123 is not found."));

        var result = await _controller.UpsertOrder(orderDto);

        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.NotNull(unprocessableResult.Value);
        _mockValidator.Verify(v => v.ValidateAsync(orderDto, default), Times.Once);
        _mockOrderService.Verify(s => s.ValidateAsync(orderDto, false), Times.Once);
        _mockOrderService.Verify(s => s.UpsertAsync(It.IsAny<OrderDto>()), Times.Never);
    }

    [Fact]
    public async Task UpsertOrder_ReturnsUnprocessableEntity_WhenUpsertFails()
    {
        var orderDto = CreateValidOrderDto();

        _mockValidator
            .Setup(v => v.ValidateAsync(orderDto, default))
            .ReturnsAsync(new ValidationResult());

        _mockOrderService
            .Setup(s => s.ValidateAsync(orderDto, false))
            .ReturnsAsync(OperationResult<OrderDto>.Success(orderDto));

        _mockOrderService
            .Setup(s => s.UpsertAsync(orderDto))
            .ReturnsAsync(OperationResult<OrderDto>.Failure("Database error occurred."));

        var result = await _controller.UpsertOrder(orderDto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
        _mockValidator.Verify(v => v.ValidateAsync(orderDto, default), Times.Once);
        _mockOrderService.Verify(s => s.ValidateAsync(orderDto, false), Times.Once);
        _mockOrderService.Verify(s => s.UpsertAsync(orderDto), Times.Once);
    }

    [Fact]
    public async Task UpsertOrder_ReturnsOk_WhenOrderIsCreatedSuccessfully()
    {
        var orderDto = CreateValidOrderDto();

        _mockValidator
            .Setup(v => v.ValidateAsync(orderDto, default))
            .ReturnsAsync(new ValidationResult());

        _mockOrderService
            .Setup(s => s.ValidateAsync(orderDto, false))
            .ReturnsAsync(OperationResult<OrderDto>.Success(orderDto));

        _mockOrderService
            .Setup(s => s.UpsertAsync(orderDto))
            .ReturnsAsync(OperationResult<OrderDto>.Success(orderDto));

        var result = await _controller.UpsertOrder(orderDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var operationResult = Assert.IsType<OperationResult<OrderDto>>(okResult.Value);
        Assert.True(operationResult.Succeeded);
        _mockValidator.Verify(v => v.ValidateAsync(orderDto, default), Times.Once);
        _mockOrderService.Verify(s => s.ValidateAsync(orderDto, false), Times.Once);
        _mockOrderService.Verify(s => s.UpsertAsync(orderDto), Times.Once);
    }

    [Fact]
    public async Task UpsertOrder_ReturnsOk_WhenOrderIsUpdatedSuccessfully()
    {
        var orderDto = CreateValidOrderDto();

        _mockValidator
            .Setup(v => v.ValidateAsync(orderDto, default))
            .ReturnsAsync(new ValidationResult());

        _mockOrderService
            .Setup(s => s.ValidateAsync(orderDto, false))
            .ReturnsAsync(OperationResult<OrderDto>.Success(orderDto));

        _mockOrderService
            .Setup(s => s.UpsertAsync(orderDto))
            .ReturnsAsync(OperationResult<OrderDto>.Success(orderDto));

        var result = await _controller.UpsertOrder(orderDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var operationResult = Assert.IsType<OperationResult<OrderDto>>(okResult.Value);
        Assert.True(operationResult.Succeeded);
        Assert.NotNull(operationResult.Payload);
        _mockValidator.Verify(v => v.ValidateAsync(orderDto, default), Times.Once);
        _mockOrderService.Verify(s => s.ValidateAsync(orderDto, false), Times.Once);
        _mockOrderService.Verify(s => s.UpsertAsync(orderDto), Times.Once);
    }

    [Fact]
    public async Task UpsertOrder_HandlesMultipleValidationErrors()
    {
        var orderDto = CreateValidOrderDto();
        var validationFailures = new List<ValidationFailure>
        {
            new ("CourtFile", "CourtFile is required."),
            new ("Referral", "Referral is required."),
            new ("PackageDocuments", "PackageDocuments cannot be empty.")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockValidator
            .Setup(v => v.ValidateAsync(orderDto, default))
            .ReturnsAsync(validationResult);

        var result = await _controller.UpsertOrder(orderDto);

        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(unprocessableResult.Value);
        Assert.Equal(3, errors.Count());
        Assert.Contains("CourtFile is required.", errors);
        Assert.Contains("Referral is required.", errors);
        Assert.Contains("PackageDocuments cannot be empty.", errors);
    }

    [Fact]
    public async Task UpsertOrder_ValidatesCourtDivisionCd()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtDivisionCd = "INVALID";

        var validationFailures = new List<ValidationFailure>
        {
            new("CourtDivisionCd", "CourtDivisionCd is unsupported.")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockValidator
            .Setup(v => v.ValidateAsync(orderDto, default))
            .ReturnsAsync(validationResult);

        var result = await _controller.UpsertOrder(orderDto);

        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(unprocessableResult.Value);
        Assert.Contains("CourtDivisionCd is unsupported.", errors);
    }

    [Fact]
    public async Task UpsertOrder_ValidatesCourtClassCd()
    {
        var orderDto = CreateValidOrderDto();
        orderDto.CourtFile.CourtClassCd = "Z";

        var validationFailures = new List<ValidationFailure>
        {
            new("CourtClassCd", "CourtClassCd is unsupported.")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockValidator
            .Setup(v => v.ValidateAsync(orderDto, default))
            .ReturnsAsync(validationResult);

        var result = await _controller.UpsertOrder(orderDto);

        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(unprocessableResult.Value);
        Assert.Contains("CourtClassCd is unsupported.", errors);
    }

    #endregion

    #region Helper Methods

    private OrderDto CreateValidOrderDto()
    {
        return new()
        {
            CourtFile = new CourtFileDto
            {
                PhysicalFileId = _faker.Random.Int(1, 10000),
                CourtDivisionCd = _faker.PickRandom("R", "I"),
                CourtClassCd = _faker.PickRandom("A", "Y", "T", "F", "C", "M", "L"),
                CourtFileNo = _faker.Random.AlphaNumeric(10)
            },
            Referral = new ReferralDto
            {
                SentToPartId = _judgeId,
                ReferredDocumentId = _faker.Random.Int(1, 10000)
            },
            PackageDocuments =
            [
                new()
                {
                    DocumentId = _faker.Random.Int(1, 10000),
                    DocumentTypeCd = _faker.Lorem.Word(),
                }
            ]
        };
    }

    #endregion
}
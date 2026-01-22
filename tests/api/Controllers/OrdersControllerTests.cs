using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bogus;
using FluentValidation;
using FluentValidation.Results;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure;
using Scv.Api.Models.Order;
using Scv.Api.Services;
using Scv.Db.Models;
using Xunit;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace tests.api.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IValidator<OrderRequestDto>> _mockOrderRequestValidator;
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly OrdersController _controller;
    private readonly Faker _faker;
    private readonly int _judgeId;

    public OrdersControllerTests()
    {
        _mockOrderRequestValidator = new Mock<IValidator<OrderRequestDto>>();
        _mockOrderService = new Mock<IOrderService>();
        _faker = new Faker();
        _judgeId = _faker.Random.Int(1, 1000);

        _controller = new OrdersController(
            _mockOrderRequestValidator.Object,
            _mockOrderService.Object);

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
                OrderRequest = new()
                {
                    CourtFile = new CourtFileDto { PhysicalFileId = 123 },
                    Referral = new ReferralDto { SentToPartId = _judgeId }
                }
            },
            new()
            {
                OrderRequest = new()
                {
                    CourtFile = new CourtFileDto { PhysicalFileId = 456 },
                    Referral = new ReferralDto { SentToPartId = _faker.Random.Int(2000, 3000) }
                }
            },
            new()
            {
                OrderRequest = new()
                {
                    CourtFile = new CourtFileDto { PhysicalFileId = 789 },
                    Referral = new ReferralDto { SentToPartId = _judgeId }
                }
            }
        };

        _mockOrderService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(orders);

        var result = await _controller.GetMyOrders();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedOrders = Assert.IsAssignableFrom<IEnumerable<OrderDto>>(okResult.Value);
        Assert.Equal(2, returnedOrders.Count());
        Assert.All(returnedOrders, order => Assert.Equal(_judgeId, order.OrderRequest.Referral.SentToPartId));
        _mockOrderService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetMyOrders_ReturnsOk_WithEmptyList_WhenNoOrdersMatch()
    {
        var orders = new List<OrderDto>
        {
            new()
            {
                OrderRequest = new()
                {
                    CourtFile = new CourtFileDto { PhysicalFileId = 123 },
                    Referral = new ReferralDto { SentToPartId = _faker.Random.Int(2000, 3000) }
                }
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
        var orderDto = CreateValidOrderRequestDto();
        var validationFailures = new List<ValidationFailure>
        {
            new("CourtFile", "CourtFile is required.")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockOrderRequestValidator
            .Setup(v => v.ValidateAsync(orderDto, default))
            .ReturnsAsync(validationResult);

        var result = await _controller.UpsertOrder(orderDto);

        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(unprocessableResult.Value);
        Assert.Contains("CourtFile is required.", errors);
        _mockOrderRequestValidator.Verify(v => v.ValidateAsync(orderDto, default), Times.Once);
        _mockOrderService.Verify(s => s.ValidateAsync(It.IsAny<OrderDto>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task UpsertOrder_ReturnsUnprocessableEntity_WhenBusinessValidationFails()
    {
        var orderRequestDto = CreateValidOrderRequestDto();

        _mockOrderRequestValidator
            .Setup(v => v.ValidateAsync(orderRequestDto, default))
            .ReturnsAsync(new ValidationResult());

        _mockOrderService
            .Setup(s => s.ValidateOrderRequestAsync(orderRequestDto))
            .ReturnsAsync(OperationResult<OrderDto>.Failure("Criminal file with id: 123 is not found."));

        var result = await _controller.UpsertOrder(orderRequestDto);

        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.NotNull(unprocessableResult.Value);
        _mockOrderRequestValidator.Verify(v => v.ValidateAsync(orderRequestDto, default), Times.Once);
        _mockOrderService.Verify(s => s.ValidateOrderRequestAsync(orderRequestDto), Times.Once);
        _mockOrderService.Verify(s => s.ProcessOrderRequestAsync(It.IsAny<OrderRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task UpsertOrder_ReturnsInternalServerError_WhenUpsertFails()
    {
        var orderRequestDto = CreateValidOrderRequestDto();

        _mockOrderRequestValidator
            .Setup(v => v.ValidateAsync(orderRequestDto, default))
            .ReturnsAsync(new ValidationResult());

        _mockOrderService
            .Setup(s => s.ValidateOrderRequestAsync(orderRequestDto))
            .ReturnsAsync(OperationResult<OrderDto>.Success(new OrderDto()));

        _mockOrderService
            .Setup(s => s.ProcessOrderRequestAsync(orderRequestDto))
            .ReturnsAsync(OperationResult<OrderDto>.Failure("Database error occurred."));

        var result = await _controller.UpsertOrder(orderRequestDto);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        _mockOrderRequestValidator.Verify(v => v.ValidateAsync(orderRequestDto, default), Times.Once);
        _mockOrderService.Verify(s => s.ValidateOrderRequestAsync(orderRequestDto), Times.Once);
        _mockOrderService.Verify(s => s.ProcessOrderRequestAsync(orderRequestDto), Times.Once);
    }

    [Fact]
    public async Task UpsertOrder_ReturnsOk_WhenOrderIsCreatedSuccessfully()
    {
        var orderRequestDto = CreateValidOrderRequestDto();

        _mockOrderRequestValidator
            .Setup(v => v.ValidateAsync(orderRequestDto, default))
            .ReturnsAsync(new ValidationResult());

        _mockOrderService
            .Setup(s => s.ValidateOrderRequestAsync(orderRequestDto))
            .ReturnsAsync(OperationResult.Success());

        _mockOrderService
            .Setup(s => s.ProcessOrderRequestAsync(orderRequestDto))
            .ReturnsAsync(OperationResult<OrderDto>.Success(new OrderDto()));

        var result = await _controller.UpsertOrder(orderRequestDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var operationResult = Assert.IsType<OperationResult<OrderDto>>(okResult.Value);
        Assert.True(operationResult.Succeeded);
        _mockOrderRequestValidator.Verify(v => v.ValidateAsync(orderRequestDto, default), Times.Once);
        _mockOrderService.Verify(s => s.ValidateOrderRequestAsync(orderRequestDto), Times.Once);
        _mockOrderService.Verify(s => s.ProcessOrderRequestAsync(orderRequestDto), Times.Once);
    }

    [Fact]
    public async Task UpsertOrder_ReturnsOk_WhenOrderIsUpdatedSuccessfully()
    {
        var orderRequestDto = CreateValidOrderRequestDto();

        _mockOrderRequestValidator
            .Setup(v => v.ValidateAsync(orderRequestDto, default))
            .ReturnsAsync(new ValidationResult());

        _mockOrderService
            .Setup(s => s.ValidateOrderRequestAsync(orderRequestDto))
            .ReturnsAsync(OperationResult.Success);

        _mockOrderService
            .Setup(s => s.ProcessOrderRequestAsync(orderRequestDto))
            .ReturnsAsync(OperationResult<OrderDto>.Success(new OrderDto()));

        var result = await _controller.UpsertOrder(orderRequestDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var operationResult = Assert.IsType<OperationResult<OrderDto>>(okResult.Value);
        Assert.True(operationResult.Succeeded);
        Assert.NotNull(operationResult.Payload);
        _mockOrderRequestValidator.Verify(v => v.ValidateAsync(orderRequestDto, default), Times.Once);
        _mockOrderService.Verify(s => s.ValidateOrderRequestAsync(orderRequestDto), Times.Once);
        _mockOrderService.Verify(s => s.ProcessOrderRequestAsync(orderRequestDto), Times.Once);
    }

    [Fact]
    public async Task UpsertOrder_HandlesMultipleValidationErrors()
    {
        var orderDto = CreateValidOrderRequestDto();
        var validationFailures = new List<ValidationFailure>
        {
            new ("CourtFile", "CourtFile is required."),
            new ("Referral", "Referral is required."),
            new ("PackageDocuments", "PackageDocuments cannot be empty.")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockOrderRequestValidator
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
        var orderDto = CreateValidOrderRequestDto();
        orderDto.CourtFile.CourtDivisionCd = "INVALID";

        var validationFailures = new List<ValidationFailure>
        {
            new("CourtDivisionCd", "CourtDivisionCd is unsupported.")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockOrderRequestValidator
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
        var orderDto = CreateValidOrderRequestDto();
        orderDto.CourtFile.CourtClassCd = "Z";

        var validationFailures = new List<ValidationFailure>
        {
            new("CourtClassCd", "CourtClassCd is unsupported.")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockOrderRequestValidator
            .Setup(v => v.ValidateAsync(orderDto, default))
            .ReturnsAsync(validationResult);

        var result = await _controller.UpsertOrder(orderDto);

        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(unprocessableResult.Value);
        Assert.Contains("CourtClassCd is unsupported.", errors);
    }

    #endregion

    #region ReviewOrder Tests

    [Fact]
    public async Task ReviewOrder_ReturnsNoContent_WhenReviewIsSuccessful()
    {
        var orderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var orderReview = new OrderReview
        {
            Status = OrderStatus.Approved,
            Comments = "Approved with no changes"
        };

        _mockOrderService
            .Setup(s => s.ReviewOrder(orderId, orderReview))
            .ReturnsAsync(OperationResult.Success());

        var result = await _controller.ReviewOrder(orderId, orderReview);

        Assert.IsType<NoContentResult>(result);
        _mockOrderService.Verify(s => s.ReviewOrder(orderId, orderReview), Times.Once);
    }

    [Fact]
    public async Task ReviewOrder_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        var orderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var orderReview = new OrderReview
        {
            Status = OrderStatus.Approved,
            Comments = "Test"
        };

        _mockOrderService
            .Setup(s => s.ReviewOrder(orderId, orderReview))
            .ReturnsAsync(OperationResult.Failure("Order not found"));

        var result = await _controller.ReviewOrder(orderId, orderReview);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
        _mockOrderService.Verify(s => s.ReviewOrder(orderId, orderReview), Times.Once);
    }

    [Fact]
    public async Task ReviewOrder_ReturnsInternalServerError_WhenJudgeNotAssigned()
    {
        var orderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var orderReview = new OrderReview
        {
            Status = OrderStatus.Approved,
            Comments = "Test"
        };

        _mockOrderService
            .Setup(s => s.ReviewOrder(orderId, orderReview))
            .ReturnsAsync(OperationResult.Failure("Judge is not assigned to review this Order."));

        var result = await _controller.ReviewOrder(orderId, orderReview);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        _mockOrderService.Verify(s => s.ReviewOrder(orderId, orderReview), Times.Once);
    }

    [Fact]
    public async Task ReviewOrder_ReturnsInternalServerError_WhenUnexpectedErrorOccurs()
    {
        var orderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var orderReview = new OrderReview
        {
            Status = OrderStatus.Approved,
            Comments = "Test"
        };

        _mockOrderService
            .Setup(s => s.ReviewOrder(orderId, orderReview))
            .ReturnsAsync(OperationResult.Failure("Database connection failed"));

        var result = await _controller.ReviewOrder(orderId, orderReview);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        _mockOrderService.Verify(s => s.ReviewOrder(orderId, orderReview), Times.Once);
    }

    [Fact]
    public async Task ReviewOrder_AcceptsApprovedStatus()
    {
        var orderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var orderReview = new OrderReview
        {
            Status = OrderStatus.Approved,
            Comments = "Looks good"
        };

        _mockOrderService
            .Setup(s => s.ReviewOrder(orderId, orderReview))
            .ReturnsAsync(OperationResult.Success());

        var result = await _controller.ReviewOrder(orderId, orderReview);

        Assert.IsType<NoContentResult>(result);
        _mockOrderService.Verify(s => s.ReviewOrder(orderId, It.Is<OrderReview>(r => 
            r.Status == OrderStatus.Approved && r.Comments == "Looks good")), Times.Once);
    }

    [Fact]
    public async Task ReviewOrder_AcceptsUnapprovedStatus()
    {
        var orderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var orderReview = new OrderReview
        {
            Status = OrderStatus.Unapproved,
            Comments = "Needs corrections"
        };

        _mockOrderService
            .Setup(s => s.ReviewOrder(orderId, orderReview))
            .ReturnsAsync(OperationResult.Success());

        var result = await _controller.ReviewOrder(orderId, orderReview);

        Assert.IsType<NoContentResult>(result);
        _mockOrderService.Verify(s => s.ReviewOrder(orderId, It.Is<OrderReview>(r => 
            r.Status == OrderStatus.Unapproved && r.Comments == "Needs corrections")), Times.Once);
    }

    [Fact]
    public async Task ReviewOrder_AcceptsNullComments()
    {
        var orderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var orderReview = new OrderReview
        {
            Status = OrderStatus.Approved,
            Comments = null
        };

        _mockOrderService
            .Setup(s => s.ReviewOrder(orderId, orderReview))
            .ReturnsAsync(OperationResult.Success());

        var result = await _controller.ReviewOrder(orderId, orderReview);

        Assert.IsType<NoContentResult>(result);
        _mockOrderService.Verify(s => s.ReviewOrder(orderId, It.Is<OrderReview>(r => 
            r.Status == OrderStatus.Approved && r.Comments == null)), Times.Once);
    }

    [Fact]
    public async Task ReviewOrder_PassesCorrectOrderIdToService()
    {
        var orderId = "507f1f77bcf86cd799439011";
        var orderReview = new OrderReview
        {
            Status = OrderStatus.Approved,
            Comments = "Test"
        };

        _mockOrderService
            .Setup(s => s.ReviewOrder(orderId, orderReview))
            .ReturnsAsync(OperationResult.Success());

        await _controller.ReviewOrder(orderId, orderReview);

        _mockOrderService.Verify(s => s.ReviewOrder(orderId, orderReview), Times.Once);
    }

    [Fact]
    public async Task ReviewOrder_HandlesEmptyOrderId()
    {
        var orderId = string.Empty;
        var orderReview = new OrderReview
        {
            Status = OrderStatus.Approved,
            Comments = "Test"
        };

        _mockOrderService
            .Setup(s => s.ReviewOrder(orderId, orderReview))
            .ReturnsAsync(OperationResult.Failure("Order not found"));

        var result = await _controller.ReviewOrder(orderId, orderReview);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task ReviewOrder_ReturnsNotFound_WhenErrorContainsNotFound()
    {
        var orderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var orderReview = new OrderReview
        {
            Status = OrderStatus.Approved,
            Comments = "Test"
        };

        _mockOrderService
            .Setup(s => s.ReviewOrder(orderId, orderReview))
            .ReturnsAsync(OperationResult.Failure("The order was not found in the database"));

        var result = await _controller.ReviewOrder(orderId, orderReview);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region Helper Methods

    private OrderRequestDto CreateValidOrderRequestDto()
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
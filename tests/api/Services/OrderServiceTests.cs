using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Bogus;
using JCCommon.Clients.FileServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Models;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models.Order;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;

public class OrderServiceTests : ServiceTestBase
{
    private readonly Faker _faker;
    private readonly Mock<IRepositoryBase<Order>> _mockOrderRepo;
    private readonly Mock<FileServicesClient> _mockFileServicesClient;
    private readonly Mock<IDashboardService> _mockDashboardService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<OrderService>> _mockLogger;
    private readonly Mock<Hangfire.IBackgroundJobClient> _mockBackgroundJobClient;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly IMapper _mapper;
    private readonly IAppCache _cache;
    private readonly OrderService _orderService;
    private readonly string _requestAgencyIdentifierId;
    private readonly string _requestPartId;
    private readonly string _applicationCode;
    private readonly int _judgeId;

    public OrderServiceTests()
    {
        _faker = new Faker();

        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));
        _cache = cachingService;

        var config = new TypeAdapterConfig();
        config.Apply(new AccessControlManagementMapping());
        _mapper = new Mapper(config);

        _mockLogger = new Mock<ILogger<OrderService>>();
        _mockOrderRepo = new Mock<IRepositoryBase<Order>>();
        _mockFileServicesClient = new Mock<FileServicesClient>(MockBehavior.Strict, this.HttpClient);
        _mockDashboardService = new Mock<IDashboardService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockBackgroundJobClient = new Mock<Hangfire.IBackgroundJobClient>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        _requestAgencyIdentifierId = _faker.Random.AlphaNumeric(10);
        _requestPartId = _faker.Random.AlphaNumeric(10);
        _applicationCode = "SCV";

        SetupConfiguration();

        _judgeId = _faker.Random.Int(1, 1000);

        _orderService = new OrderService(
            _cache,
            _mapper,
            _mockLogger.Object,
            _mockOrderRepo.Object,
            _mockFileServicesClient.Object,
            _mockConfiguration.Object,
            _mockDashboardService.Object,
            _mockBackgroundJobClient.Object,
            _mockHttpContextAccessor.Object);
    }

    private void SetupConfiguration()
    {
        var mockAppCodeSection = new Mock<IConfigurationSection>();
        mockAppCodeSection.Setup(s => s.Value).Returns(_applicationCode);
        _mockConfiguration.Setup(c => c.GetSection("Request:ApplicationCd")).Returns(mockAppCodeSection.Object);

        var mockAgencySection = new Mock<IConfigurationSection>();
        mockAgencySection.Setup(s => s.Value).Returns(_requestAgencyIdentifierId);
        _mockConfiguration.Setup(c => c.GetSection("Request:AgencyIdentifierId")).Returns(mockAgencySection.Object);

        var mockPartIdSection = new Mock<IConfigurationSection>();
        mockPartIdSection.Setup(s => s.Value).Returns(_requestPartId);
        _mockConfiguration.Setup(c => c.GetSection("Request:PartId")).Returns(mockPartIdSection.Object);
    }

    #region GetJudgeOrders Tests

    [Fact]
    public async Task GetJudgeOrdersAsync_ReturnsAllOrders()
    {
        var orders = new List<Order>
        {
            CreateOrder(),
            CreateOrder()
        };

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync(orders);

        var result = await _orderService.GetJudgeOrdersAsync(_judgeId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockOrderRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetJudgeOrdersAsync_ReturnsEmptyList_WhenNoOrders()
    {
        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([]);

        var result = await _orderService.GetJudgeOrdersAsync(_judgeId);

        Assert.NotNull(result);
        Assert.Empty(result);
        _mockOrderRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()), Times.Once);
    }

    #endregion

    #region ValidateOrderRequestAsync Tests - Criminal Files

    [Fact]
    public async Task ValidateOrderRequestAsync_ReturnsFailure_WhenInvalidCourtClassCd()
    {
        var orderDto = CreateValidOrderRequestDto();
        orderDto.CourtFile.CourtClassCd = "INVALID";

        var result = await _orderService.ValidateOrderRequestAsync(orderDto);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid CourtClassCd: INVALID", result.Errors);
    }

    [Fact]
    public async Task ValidateOrderRequestAsync_ReturnsFailure_WhenCriminalFileNotFound()
    {
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.CourtFile.CourtClassCd = "A";

        _mockFileServicesClient
            .Setup(c => c.FilesCriminalFilecontentAsync(
                _requestAgencyIdentifierId,
                _requestPartId,
                _applicationCode,
                null, null, null, null,
                orderRequestDto.CourtFile.PhysicalFileId.ToString()))
            .ReturnsAsync((CriminalFileContent)null);

        var result = await _orderService.ValidateOrderRequestAsync(orderRequestDto);

        Assert.False(result.Succeeded);
        Assert.Contains($"Criminal file with id: {orderRequestDto.CourtFile.PhysicalFileId} is not found.", result.Errors);
    }

    [Fact]
    public async Task ValidateOrderRequestAsync_ReturnsFailure_WhenCriminalFileHasNoAccusedFiles()
    {
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.CourtFile.CourtClassCd = "A";

        _mockFileServicesClient
            .Setup(c => c.FilesCriminalFilecontentAsync(
                _requestAgencyIdentifierId,
                _requestPartId,
                _applicationCode,
                null, null, null, null,
                orderRequestDto.CourtFile.PhysicalFileId.ToString()))
            .ReturnsAsync(new CriminalFileContent { AccusedFile = [] });

        var result = await _orderService.ValidateOrderRequestAsync(orderRequestDto);

        Assert.False(result.Succeeded);
        Assert.Contains($"Criminal file with id: {orderRequestDto.CourtFile.PhysicalFileId} is not found.", result.Errors);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Y")]
    [InlineData("T")]
    public async Task ValidateOrderRequestAsync_ValidatesCriminalFile_ForCriminalCourtClasses(string courtClass)
    {
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.CourtFile.CourtClassCd = courtClass;
        var judgeId = _faker.Random.Int(1, 1000);

        _mockFileServicesClient
            .Setup(c => c.FilesCriminalFilecontentAsync(
                _requestAgencyIdentifierId,
                _requestPartId,
                _applicationCode,
                null, null, null, null,
                orderRequestDto.CourtFile.PhysicalFileId.ToString()))
            .ReturnsAsync(new CriminalFileContent { AccusedFile = [new()] });

        _mockDashboardService
            .Setup(d => d.GetJudges())
            .ReturnsAsync([new PersonSearchItem { PersonId = judgeId }]);

        orderRequestDto.Referral.SentToPartId = judgeId;

        var result = await _orderService.ValidateOrderRequestAsync(orderRequestDto);

        Assert.True(result.Succeeded);
        _mockFileServicesClient.Verify(c => c.FilesCriminalFilecontentAsync(
            _requestAgencyIdentifierId,
            _requestPartId,
            _applicationCode,
            null, null, null, null,
            orderRequestDto.CourtFile.PhysicalFileId.ToString()), Times.Once);
    }

    #endregion

    #region ValidateOrderRequestAsync Tests - Civil Files

    [Fact]
    public async Task ValidateOrderRequestAsync_ReturnsFailure_WhenCivilFileNotFound()
    {
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.CourtFile.CourtClassCd = "C";

        _mockFileServicesClient
            .Setup(c => c.FilesCivilFilecontentAsync(
                _requestAgencyIdentifierId,
                _requestPartId,
                _applicationCode,
                null, null, null, null,
                orderRequestDto.CourtFile.PhysicalFileId.ToString()))
            .ReturnsAsync((CivilFileContent)null);

        var result = await _orderService.ValidateOrderRequestAsync(orderRequestDto);

        Assert.False(result.Succeeded);
        Assert.Contains($"Civil file with id: {orderRequestDto.CourtFile.PhysicalFileId} is not found.", result.Errors);
    }

    [Fact]
    public async Task ValidateOrderRequestAsync_ReturnsFailure_WhenCivilFileHasNoCivilFiles()
    {
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.CourtFile.CourtClassCd = "F";

        _mockFileServicesClient
            .Setup(c => c.FilesCivilFilecontentAsync(
                _requestAgencyIdentifierId,
                _requestPartId,
                _applicationCode,
                null, null, null, null,
                orderRequestDto.CourtFile.PhysicalFileId.ToString()))
            .ReturnsAsync(new CivilFileContent { CivilFile = [] });

        var result = await _orderService.ValidateOrderRequestAsync(orderRequestDto);

        Assert.False(result.Succeeded);
        Assert.Contains($"Civil file with id: {orderRequestDto.CourtFile.PhysicalFileId} is not found.", result.Errors);
    }

    [Theory]
    [InlineData("C")]
    [InlineData("F")]
    [InlineData("L")]
    [InlineData("M")]
    public async Task ValidateOrderRequestAsync_ValidatesCivilFile_ForCivilCourtClasses(string courtClass)
    {
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.CourtFile.CourtClassCd = courtClass;
        var judgeId = _faker.Random.Int(1, 1000);

        _mockFileServicesClient
            .Setup(c => c.FilesCivilFilecontentAsync(
                _requestAgencyIdentifierId,
                _requestPartId,
                _applicationCode,
                null, null, null, null,
                orderRequestDto.CourtFile.PhysicalFileId.ToString()))
            .ReturnsAsync(new CivilFileContent { CivilFile = [new()] });

        _mockDashboardService
            .Setup(d => d.GetJudges())
            .ReturnsAsync([new() { PersonId = judgeId }]);

        orderRequestDto.Referral.SentToPartId = judgeId;

        var result = await _orderService.ValidateOrderRequestAsync(orderRequestDto);

        Assert.True(result.Succeeded);
        _mockFileServicesClient.Verify(c => c.FilesCivilFilecontentAsync(
            _requestAgencyIdentifierId,
            _requestPartId,
            _applicationCode,
            null, null, null, null,
            orderRequestDto.CourtFile.PhysicalFileId.ToString()), Times.Once);
    }

    [Fact]
    public async Task ValidateOrderRequestAsync_ReturnsFailure_WhenUnsupportedCourtClass()
    {
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.CourtFile.CourtClassCd = "B";

        var result = await _orderService.ValidateOrderRequestAsync(orderRequestDto);

        Assert.False(result.Succeeded);
        Assert.Contains("Unsupported CourtClassCd: B.", result.Errors);
    }

    #endregion

    #region ValidateOrderRequestAsync Tests - Judge Validation

    [Fact]
    public async Task ValidateOrderRequestAsync_ReturnsFailure_WhenJudgeNotFound()
    {
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.CourtFile.CourtClassCd = "A";

        _mockFileServicesClient
            .Setup(c => c.FilesCriminalFilecontentAsync(
                _requestAgencyIdentifierId,
                _requestPartId,
                _applicationCode,
                null, null, null, null,
                orderRequestDto.CourtFile.PhysicalFileId.ToString()))
            .ReturnsAsync(new CriminalFileContent { AccusedFile = [new()] });

        _mockDashboardService
            .Setup(d => d.GetJudges())
            .ReturnsAsync([]);

        var result = await _orderService.ValidateOrderRequestAsync(orderRequestDto);

        Assert.False(result.Succeeded);
        Assert.Contains($"Judge with id: {orderRequestDto.Referral.SentToPartId} is not found.", result.Errors);
    }

    [Fact]
    public async Task ValidateOrderRequestAsync_ReturnsSuccess_WhenJudgeExists()
    {
        var orderRequestDto = CreateValidOrderRequestDto();
        orderRequestDto.CourtFile.CourtClassCd = "A";
        var judgeId = orderRequestDto.Referral.SentToPartId.GetValueOrDefault();

        _mockFileServicesClient
            .Setup(c => c.FilesCriminalFilecontentAsync(
                _requestAgencyIdentifierId,
                _requestPartId,
                _applicationCode,
                null, null, null, null,
                orderRequestDto.CourtFile.PhysicalFileId.ToString()))
            .ReturnsAsync(new CriminalFileContent { AccusedFile = [new()] });

        _mockDashboardService
            .Setup(d => d.GetJudges())
            .ReturnsAsync([new() { PersonId = judgeId }]);

        var result = await _orderService.ValidateOrderRequestAsync(orderRequestDto);

        Assert.True(result.Succeeded);
    }

    #endregion

    #region ProcessOrderRequestAsync Tests

    [Fact]
    public async Task ProcessOrderRequestAsync_CreatesNewOrder_WhenOrderDoesNotExist()
    {
        var orderRequestDto = CreateValidOrderRequestDto();

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([]);

        _mockOrderRepo
            .Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        var result = await _orderService.ProcessOrderRequestAsync(orderRequestDto);

        Assert.True(result.Succeeded);
        _mockOrderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        _mockOrderRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task ProcessOrderRequestAsync_UpdatesExistingOrder_WhenOrderExists()
    {
        var orderRequestDto = CreateValidOrderRequestDto();
        var existingOrder = CreateOrder();
        existingOrder.OrderRequest.CourtFile.PhysicalFileId = orderRequestDto.CourtFile.PhysicalFileId;
        existingOrder.OrderRequest.Referral.SentToPartId = orderRequestDto.Referral.SentToPartId;
        existingOrder.OrderRequest.Referral.ReferredDocumentId = orderRequestDto.Referral.ReferredDocumentId;

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([existingOrder]);

        _mockOrderRepo
            .Setup(r => r.GetByIdAsync(existingOrder.Id))
            .ReturnsAsync(existingOrder);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        var result = await _orderService.ProcessOrderRequestAsync(orderRequestDto);

        Assert.True(result.Succeeded);
        Assert.Equal(existingOrder.Id, result.Payload.Id);
        _mockOrderRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _mockOrderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task ProcessOrderRequestAsync_SetsIdFromExistingOrder_BeforeUpdate()
    {
        var orderDto = CreateValidOrderRequestDto();
        var existingOrderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var existingOrder = CreateOrder();
        existingOrder.Id = existingOrderId;
        existingOrder.OrderRequest.CourtFile.PhysicalFileId = orderDto.CourtFile.PhysicalFileId;
        existingOrder.OrderRequest.Referral.SentToPartId = orderDto.Referral.SentToPartId;
        existingOrder.OrderRequest.Referral.ReferredDocumentId = orderDto.Referral.ReferredDocumentId;

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([existingOrder]);

        _mockOrderRepo
            .Setup(r => r.GetByIdAsync(existingOrderId))
            .ReturnsAsync(existingOrder);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        var result = await _orderService.ProcessOrderRequestAsync(orderDto);

        Assert.True(result.Succeeded);
        Assert.Equal(existingOrderId, result.Payload.Id);
    }

    [Fact]
    public async Task ProcessOrderRequestAsync_ReturnsFailure_WhenExceptionThrown()
    {
        var orderRequestDto = CreateValidOrderRequestDto();

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await _orderService.ProcessOrderRequestAsync(orderRequestDto);

        Assert.False(result.Succeeded);
        Assert.Contains("Something went wrong when upserting the Order", result.Errors);
    }

    [Fact]
    public async Task ProcessOrderRequestAsync_LogsInformation_WhenCreatingNewOrder()
    {
        var orderRequestDto = CreateValidOrderRequestDto();

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([]);

        _mockOrderRepo
            .Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        await _orderService.ProcessOrderRequestAsync(orderRequestDto);

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Creating new order")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessOrderRequestAsync_LogsInformation_WhenUpdatingExistingOrder()
    {
        var orderDto = CreateValidOrderRequestDto();
        var existingOrder = CreateOrder();
        existingOrder.OrderRequest.CourtFile.PhysicalFileId = orderDto.CourtFile.PhysicalFileId;
        existingOrder.OrderRequest.Referral.SentToPartId = orderDto.Referral.SentToPartId;
        existingOrder.OrderRequest.Referral.ReferredDocumentId = orderDto.Referral.ReferredDocumentId;

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([existingOrder]);

        _mockOrderRepo
            .Setup(r => r.GetByIdAsync(existingOrder.Id))
            .ReturnsAsync(existingOrder);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        await _orderService.ProcessOrderRequestAsync(orderDto);

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Updating existing order")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessOrderRequestAsync_LogsError_WhenExceptionOccurs()
    {
        var orderRequestDto = CreateValidOrderRequestDto();

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ThrowsAsync(new Exception("Database error"));

        await _orderService.ProcessOrderRequestAsync(orderRequestDto);

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Something went wrong when upserting the Order")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessOrderRequestAsync_EnqueuesNotificationJob_WhenCreatingNewOrder()
    {
        var orderRequestDto = CreateValidOrderRequestDto();

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([]);

        _mockOrderRepo
            .Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        var result = await _orderService.ProcessOrderRequestAsync(orderRequestDto);

        Assert.True(result.Succeeded);
        _mockBackgroundJobClient.Verify(c => c.Create(
            It.IsAny<Hangfire.Common.Job>(),
            It.IsAny<Hangfire.States.IState>()), Times.Once);
    }

    [Fact]
    public async Task ProcessOrderRequestAsync_DoesNotEnqueueNotificationJob_WhenCreationFails()
    {
        var orderRequestDto = CreateValidOrderRequestDto();

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([]);

        _mockOrderRepo
            .Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await _orderService.ProcessOrderRequestAsync(orderRequestDto);

        Assert.False(result.Succeeded);
        _mockBackgroundJobClient.Verify(c => c.Create(
            It.IsAny<Hangfire.Common.Job>(),
            It.IsAny<Hangfire.States.IState>()), Times.Never);
    }

    [Fact]
    public async Task ProcessOrderRequestAsync_DoesNotEnqueueNotificationJob_WhenUpdatingExistingOrder()
    {
        var orderRequestDto = CreateValidOrderRequestDto();
        var existingOrder = CreateOrder();
        existingOrder.OrderRequest.CourtFile.PhysicalFileId = orderRequestDto.CourtFile.PhysicalFileId;
        existingOrder.OrderRequest.Referral.SentToPartId = orderRequestDto.Referral.SentToPartId;
        existingOrder.OrderRequest.Referral.ReferredDocumentId = orderRequestDto.Referral.ReferredDocumentId;

        _mockOrderRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
            .ReturnsAsync([existingOrder]);

        _mockOrderRepo
            .Setup(r => r.GetByIdAsync(existingOrder.Id))
            .ReturnsAsync(existingOrder);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        var result = await _orderService.ProcessOrderRequestAsync(orderRequestDto);

        Assert.True(result.Succeeded);
        _mockBackgroundJobClient.Verify(c => c.Create(
            It.IsAny<Hangfire.Common.Job>(),
            It.IsAny<Hangfire.States.IState>()), Times.Never);
    }

    #endregion

    #region ReviewOrder Tests

    [Fact]
    public async Task ReviewOrder_ReturnsFailure_WhenOrderNotFound()
    {
        var orderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var orderReview = new OrderReviewDto { Status = OrderStatus.Approved };

        _mockOrderRepo
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order)null);

        var result = await _orderService.ReviewOrder(orderId, orderReview);

        Assert.False(result.Succeeded);
        Assert.Contains("Order not found", result.Errors);
        _mockOrderRepo.Verify(r => r.GetByIdAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task ReviewOrder_ReturnsFailure_WhenJudgeNotAssignedToOrder()
    {
        var orderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var assignedJudgeId = _faker.Random.Int(1, 1000);
        var differentJudgeId = assignedJudgeId + 1;
        var orderReview = new OrderReviewDto { Status = OrderStatus.Approved };

        var order = CreateOrder();
        order.Id = orderId;
        order.OrderRequest.Referral.SentToPartId = assignedJudgeId;

        _mockOrderRepo
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        SetupHttpContextWithJudge(differentJudgeId);

        var result = await _orderService.ReviewOrder(orderId, orderReview);

        Assert.False(result.Succeeded);
        Assert.Contains("Judge is not assigned to review this Order.", result.Errors);
    }

    [Fact]
    public async Task ReviewOrder_ReturnsSuccess_WhenJudgeIsAssignedAndReviewIsValid()
    {
        var orderId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var judgeId = _faker.Random.Int(1, 1000);
        var orderReview = new OrderReviewDto
        {
            Status = OrderStatus.Approved,
            Comments = "Test notes"
        };

        var order = CreateOrder();
        order.Id = orderId;
        order.OrderRequest.Referral.SentToPartId = judgeId;

        _mockOrderRepo
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        _mockOrderRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        SetupHttpContextWithJudge(judgeId);

        var result = await _orderService.ReviewOrder(orderId, orderReview);

        Assert.True(result.Succeeded);
        _mockOrderRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private OrderRequestDto CreateValidOrderRequestDto()
    {
        return new OrderRequestDto
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
                SentToPartId = _faker.Random.Int(1, 1000),
                ReferredDocumentId = _faker.Random.Int(1, 1000)
            },
            PackageDocuments =
            [
                new()
                {
                    DocumentId = _faker.Random.Int(1, 1000),
                    DocumentTypeCd = _faker.Lorem.Word()
                }
            ]
        };
    }

    private Order CreateOrder()
    {
        return new Order
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OrderRequest = new OrderRequest
            {
                CourtFile = new CourtFile
                {
                    PhysicalFileId = _faker.Random.Int(1, 10000),
                    CourtDivisionCd = _faker.PickRandom("R", "I"),
                    CourtClassCd = _faker.PickRandom("A", "Y", "T", "F", "C", "M", "L"),
                    CourtFileNo = _faker.Random.AlphaNumeric(10)
                },
                Referral = new Referral
                {
                    SentToPartId = _faker.Random.Int(1, 1000),
                    ReferredDocumentId = _faker.Random.Int(1, 1000)
                },
                PackageDocuments =
                [
                    new()
                    {
                        DocumentId = _faker.Random.Int(1, 1000),
                        DocumentTypeCd = _faker.Lorem.Word()
                    }
                ]
            }
        };
    }

    private void SetupHttpContextWithJudge(int judgeId)
    {
        var claims = new List<Claim>
        {
            new Claim(Scv.Api.Helpers.CustomClaimTypes.JudgeId, judgeId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);
    }

    #endregion
}
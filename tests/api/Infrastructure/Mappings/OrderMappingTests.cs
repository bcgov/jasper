using System;
using System.Globalization;
using Bogus;
using Mapster;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models.Order;
using Scv.Db.Models;
using Xunit;
using PCSSCommonConstants = PCSSCommon.Common.Constants;

namespace tests.api.Infrastructure.Mappings;

public class OrderMappingTests
{
    private readonly TypeAdapterConfig _config;
    private readonly Faker _faker;

    public OrderMappingTests()
    {
        _faker = new Faker();
        _config = new TypeAdapterConfig();
        new OrderMapping().Register(_config);
    }

    #region Order -> OrderDto Mapping Tests

    [Fact]
    public void Order_To_OrderDto_MapsAllProperties()
    {
        var order = CreateOrder();

        var result = order.Adapt<OrderDto>(_config);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal(order.Status, result.Status);
        Assert.Equal(order.Signed, result.Signed);
        Assert.Equal(order.Comments, result.Comments);
        Assert.Equal(order.DocumentData, result.DocumentData);
        Assert.Equal(order.ProcessedDate, result.ProcessedDate);
        Assert.NotNull(result.OrderRequest);
    }

    [Fact]
    public void Order_To_OrderDto_MapsCreatedDateFromEnt_Dtm()
    {
        var expectedDate = _faker.Date.Past();
        var order = CreateOrder();
        order.Ent_Dtm = expectedDate;

        var result = order.Adapt<OrderDto>(_config);

        Assert.Equal(expectedDate, result.CreatedDate);
    }

    [Fact]
    public void Order_To_OrderDto_MapsUpdatedDateFromUpd_Dtm()
    {
        var expectedDate = _faker.Date.Recent();
        var order = CreateOrder();
        order.Upd_Dtm = expectedDate;

        var result = order.Adapt<OrderDto>(_config);

        Assert.Equal(expectedDate, result.UpdatedDate);
    }

    [Fact]
    public void Order_To_OrderDto_MapsNestedOrderRequest()
    {
        var order = CreateOrder();

        var result = order.Adapt<OrderDto>(_config);

        Assert.NotNull(result.OrderRequest);
        Assert.NotNull(result.OrderRequest.CourtFile);
        Assert.Equal(order.OrderRequest.CourtFile.PhysicalFileId, result.OrderRequest.CourtFile.PhysicalFileId);
        Assert.NotNull(result.OrderRequest.Referral);
        Assert.Equal(order.OrderRequest.Referral.SentToPartId, result.OrderRequest.Referral.SentToPartId);
    }

    #endregion

    #region OrderDto -> Order Mapping Tests

    [Fact]
    public void OrderDto_To_Order_MapsAllNonIgnoredProperties()
    {
        var orderDto = CreateOrderDto();

        var result = orderDto.Adapt<Order>(_config);

        Assert.NotNull(result);
        Assert.Equal(orderDto.Status, result.Status);
        Assert.Equal(orderDto.Signed, result.Signed);
        Assert.Equal(orderDto.Comments, result.Comments);
        Assert.Equal(orderDto.DocumentData, result.DocumentData);
        Assert.Equal(orderDto.ProcessedDate, result.ProcessedDate);
        Assert.NotNull(result.OrderRequest);
    }

    [Fact]
    public void OrderDto_To_Order_IgnoresId()
    {
        var orderDto = CreateOrderDto();
        orderDto.Id = "should-be-ignored";

        var result = orderDto.Adapt<Order>(_config);

        Assert.NotEqual("should-be-ignored", result.Id);
    }

    [Fact]
    public void OrderDto_To_Order_IgnoresEnt_Dtm()
    {
        var orderDto = CreateOrderDto();
        orderDto.CreatedDate = _faker.Date.Past();

        var result = orderDto.Adapt<Order>(_config);

        Assert.Equal(default(DateTime), result.Ent_Dtm);
    }

    [Fact]
    public void OrderDto_To_Order_IgnoresEnt_UserId()
    {
        var orderDto = CreateOrderDto();

        var result = orderDto.Adapt<Order>(_config);

        Assert.Null(result.Ent_UserId);
    }

    [Fact]
    public void OrderDto_To_Order_IgnoresUpd_Dtm()
    {
        var orderDto = CreateOrderDto();
        orderDto.UpdatedDate = _faker.Date.Recent();

        var result = orderDto.Adapt<Order>(_config);

        Assert.Equal(default(DateTime), result.Upd_Dtm);
    }

    [Fact]
    public void OrderDto_To_Order_IgnoresUpd_UserId()
    {
        var orderDto = CreateOrderDto();

        var result = orderDto.Adapt<Order>(_config);

        Assert.Null(result.Upd_UserId);
    }

    #endregion

    #region OrderReviewDto -> OrderDto Mapping Tests

    [Fact]
    public void OrderReviewDto_To_OrderDto_MapsStatus()
    {
        var orderReviewDto = new OrderReviewDto
        {
            Status = OrderStatus.Approved,
            Comments = _faker.Lorem.Sentence()
        };

        var result = orderReviewDto.Adapt<OrderDto>(_config);

        Assert.Equal(OrderStatus.Approved, result.Status);
    }

    [Fact]
    public void OrderReviewDto_To_OrderDto_MapsComments()
    {
        var expectedComments = _faker.Lorem.Paragraph();
        var orderReviewDto = new OrderReviewDto
        {
            Status = OrderStatus.Approved,
            Comments = expectedComments
        };

        var result = orderReviewDto.Adapt<OrderDto>(_config);

        Assert.Equal(expectedComments, result.Comments);
    }

    [Fact]
    public void OrderReviewDto_To_OrderDto_SetsProcessedDateToUtcNow()
    {
        var orderReviewDto = new OrderReviewDto
        {
            Status = OrderStatus.Approved,
            Comments = _faker.Lorem.Sentence()
        };
        var beforeMapping = DateTime.UtcNow;

        var result = orderReviewDto.Adapt<OrderDto>(_config);

        var afterMapping = DateTime.UtcNow;
        Assert.NotNull(result.ProcessedDate);
        Assert.InRange(result.ProcessedDate.Value, beforeMapping, afterMapping);
    }

    [Fact]
    public void OrderReviewDto_To_OrderDto_SetsUpdatedDateToUtcNow()
    {
        var orderReviewDto = new OrderReviewDto
        {
            Status = OrderStatus.Approved,
            Comments = _faker.Lorem.Sentence()
        };
        var beforeMapping = DateTime.UtcNow;

        var result = orderReviewDto.Adapt<OrderDto>(_config);

        var afterMapping = DateTime.UtcNow;
        Assert.NotNull(result.UpdatedDate);
        Assert.InRange(result.UpdatedDate.Value, beforeMapping, afterMapping);
    }

    [Fact]
    public void OrderReviewDto_To_OrderDto_IgnoresNullValues()
    {
        var orderReviewDto = new OrderReviewDto
        {
            Status = OrderStatus.Approved,
            Comments = null
        };

        var result = orderReviewDto.Adapt<OrderDto>(_config);

        Assert.Null(result.Comments);
    }

    [Fact]
    public void OrderReviewDto_To_OrderDto_MapsSigned()
    {
        var orderReviewDto = new OrderReviewDto
        {
            Status = OrderStatus.Approved,
            Signed = true
        };

        var result = orderReviewDto.Adapt<OrderDto>(_config);

        Assert.True(result.Signed);
    }

    #endregion

    #region Order -> OrderViewDto Mapping Tests

    [Fact]
    public void Order_To_OrderViewDto_MapsCourtFileNumber()
    {
        var expectedFileNumber = "12345-01";
        var order = CreateOrder();
        order.OrderRequest.CourtFile.FullFileNo = expectedFileNumber;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(expectedFileNumber, result.CourtFileNumber);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsPhysicalFileId()
    {
        var expectedId = _faker.Random.Int(100, 9999);
        var order = CreateOrder();
        order.OrderRequest.CourtFile.PhysicalFileId = expectedId;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(expectedId, result.PhysicalFileId);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsCourtClass()
    {
        var expectedCourtClass = "A";
        var order = CreateOrder();
        order.OrderRequest.CourtFile.CourtClassCd = expectedCourtClass;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(expectedCourtClass, result.CourtClass);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsStyleOfCause()
    {
        var expectedStyle = "Smith v. Jones";
        var order = CreateOrder();
        order.OrderRequest.CourtFile.StyleOfCause = expectedStyle;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(expectedStyle, result.StyleOfCause);
    }

    [Fact]
    public void Order_To_OrderViewDto_FormatsReceivedDateCorrectly()
    {
        var date = new DateTime(2026, 1, 23, 10, 30, 0, DateTimeKind.Utc);
        var expectedFormat = date.ToString(PCSSCommonConstants.DATE_FORMAT, CultureInfo.InvariantCulture);
        var order = CreateOrder();
        order.Ent_Dtm = date;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(expectedFormat, result.ReceivedDate);
    }

    [Fact]
    public void Order_To_OrderViewDto_FormatsProcessedDateCorrectly_WhenNotNull()
    {
        var date = new DateTime(2026, 1, 25, 14, 45, 0, DateTimeKind.Utc);
        var expectedFormat = date.ToString(PCSSCommonConstants.DATE_FORMAT, CultureInfo.InvariantCulture);
        var order = CreateOrder();
        order.ProcessedDate = date;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(expectedFormat, result.ProcessedDate);
    }

    [Fact]
    public void Order_To_OrderViewDto_ReturnsNullProcessedDate_WhenNull()
    {
        var order = CreateOrder();
        order.ProcessedDate = null;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Null(result.ProcessedDate);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsStatus()
    {
        var order = CreateOrder();
        order.Status = OrderStatus.Approved;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(OrderStatus.Approved, result.Status);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsId()
    {
        var expectedId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var order = CreateOrder();
        order.Id = expectedId;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(expectedId, result.Id);
    }

    #endregion

    #region Helper Methods

    private Order CreateOrder()
    {
        return new Order
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OrderRequest = new OrderRequest
            {
                CourtFile = new CourtFile
                {
                    PhysicalFileId = _faker.Random.Int(100, 9999),
                    CourtDivisionCd = _faker.PickRandom("R", "I"),
                    CourtClassCd = _faker.PickRandom("A", "Y", "T", "F", "C", "M", "L"),
                    CourtFileNo = _faker.Random.Replace("####-##"),
                    FullFileNo = _faker.Random.Replace("####-##"),
                    StyleOfCause = $"{_faker.Name.LastName()} v. {_faker.Name.LastName()}",
                    Parties = []
                },
                Referral = new Referral
                {
                    SentToPartId = _faker.Random.Int(1, 1000),
                    SentToName = _faker.Name.FullName(),
                    ReferredDocumentId = _faker.Random.Int(1, 1000)
                },
                PackageDocuments = [],
                RelevantCeisDocuments = []
            },
            Status = _faker.PickRandom<OrderStatus>(),
            Signed = _faker.Random.Bool(),
            Comments = _faker.Lorem.Sentence(),
            DocumentData = _faker.Lorem.Paragraph(),
            ProcessedDate = _faker.Date.Recent(),
            Ent_Dtm = _faker.Date.Past(),
            Upd_Dtm = _faker.Date.Recent(),
            Ent_UserId = _faker.Random.AlphaNumeric(10),
            Upd_UserId = _faker.Random.AlphaNumeric(10)
        };
    }

    private OrderDto CreateOrderDto()
    {
        return new OrderDto
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OrderRequest = new OrderRequestDto
            {
                CourtFile = new CourtFileDto
                {
                    PhysicalFileId = _faker.Random.Int(100, 9999),
                    CourtDivisionCd = _faker.PickRandom("R", "I"),
                    CourtClassCd = _faker.PickRandom("A", "Y", "T", "F", "C", "M", "L"),
                    CourtFileNo = _faker.Random.Replace("####-##"),
                    FullFileNo = _faker.Random.Replace("####-##"),
                    StyleOfCause = $"{_faker.Name.LastName()} v. {_faker.Name.LastName()}",
                    Parties = []
                },
                Referral = new ReferralDto
                {
                    SentToPartId = _faker.Random.Int(1, 1000),
                    SentToName = _faker.Name.FullName(),
                    ReferredDocumentId = _faker.Random.Int(1, 1000)
                },
                PackageDocuments = [],
                RelevantCeisDocuments = []
            },
            Status = _faker.PickRandom<OrderStatus>(),
            Signed = _faker.Random.Bool(),
            Comments = _faker.Lorem.Sentence(),
            DocumentData = _faker.Lorem.Paragraph(),
            ProcessedDate = _faker.Date.Recent(),
            CreatedDate = _faker.Date.Past(),
            UpdatedDate = _faker.Date.Recent()
        };
    }

    #endregion
}
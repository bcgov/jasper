using System;
using System.Globalization;
using Bogus;
using CSOCommon.Models;
using Mapster;
using Scv.Api.Infrastructure.Mappings;
using Scv.Db.Models;
using Scv.Models.Order;
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
        OrderMapping.Register(_config);
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
        Assert.Equal(Convert.ToBase64String(order.DocumentData), result.DocumentData);
        Assert.Equal(Convert.ToBase64String(order.SupportingDocumentData), result.SupportingDocumentData);
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
        Assert.Equal(order.OrderRequest.PhysicalFileId, result.OrderRequest.PhysicalFileId);
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
        Assert.Equal(Convert.FromBase64String(orderDto.DocumentData), result.DocumentData);
        Assert.Equal(Convert.FromBase64String(orderDto.SupportingDocumentData), result.SupportingDocumentData);
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

        Assert.Equal(default, result.Ent_Dtm);
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

        Assert.Equal(default, result.Upd_Dtm);
    }

    [Fact]
    public void OrderDto_To_Order_IgnoresUpd_UserId()
    {
        var orderDto = CreateOrderDto();

        var result = orderDto.Adapt<Order>(_config);

        Assert.Null(result.Upd_UserId);
    }

    [Fact]
    public void OrderDto_To_Order_DoesNotSetReminderNotificationsSent()
    {
        var orderDto = CreateOrderDto();

        var result = orderDto.Adapt<Order>(_config);

        Assert.Equal(0, result.ReminderNotificationsSent);
    }

    [Fact]
    public void OrderDto_To_Order_DoesNotSetReassignmentNotificationsSent()
    {
        var orderDto = CreateOrderDto();

        var result = orderDto.Adapt<Order>(_config);

        Assert.Equal(0, result.ReassignmentNotificationsSent);
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

    [Fact]
    public void OrderReviewDto_To_OrderDto_MapsSupportingDocumentData()
    {
        var supportingDocumentData = Convert.ToBase64String(_faker.Random.Bytes(64));
        var orderReviewDto = new OrderReviewDto
        {
            Status = OrderStatus.Approved,
            SupportingDocumentData = supportingDocumentData
        };

        var result = orderReviewDto.Adapt<OrderDto>(_config);

        Assert.Equal(supportingDocumentData, result.SupportingDocumentData);
    }

    #endregion

    #region Order -> OrderViewDto Mapping Tests

    [Fact]
    public void Order_To_OrderViewDto_MapsCourtFileNumber()
    {
        var expectedFileNumber = "12345-01";
        var order = CreateOrder();
        order.OrderRequest.FullFileNo = expectedFileNumber;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(expectedFileNumber, result.CourtFileNumber);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsPhysicalFileId()
    {
        var expectedId = _faker.Random.Int(100, 9999);
        var order = CreateOrder();
        order.OrderRequest.PhysicalFileId = expectedId;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(expectedId, result.PhysicalFileId);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsCourtClass()
    {
        var expectedCourtClass = "A";
        var order = CreateOrder();
        order.OrderRequest.CourtClassCd = expectedCourtClass;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(expectedCourtClass, result.CourtClass);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsPriorityType()
    {
        var expectedPriorityType = "PRO";
        var order = CreateOrder();
        order.OrderRequest.Referral.PriorityType = expectedPriorityType;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(expectedPriorityType, result.PriorityType);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsCourtListType_PSM_To_Order()
    {
        var order = CreateOrder();
        order.OrderRequest.Referral.CourtListTypeCd = "PSM";

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal("Desk Order", result.CourtListTypeDescription);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsCourtListType_PSC_To_DeskOrder()
    {
        var order = CreateOrder();
        order.OrderRequest.Referral.CourtListTypeCd = "PSC";

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal("Order", result.CourtListTypeDescription);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsCourtListType_PFA_To_Order()
    {
        var order = CreateOrder();
        order.OrderRequest.Referral.CourtListTypeCd = "PFA";

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal("Order", result.CourtListTypeDescription);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsCourtListType_PFM_To_DeskOrder()
    {
        var order = CreateOrder();
        order.OrderRequest.Referral.CourtListTypeCd = "PFM";

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal("Desk Order", result.CourtListTypeDescription);
    }

    [Fact]
    public void Order_To_OrderViewDto_MapsCourtListType_UnknownCode_PassesThrough()
    {
        var unknownCode = "XYZ";
        var order = CreateOrder();
        order.OrderRequest.Referral.CourtListTypeCd = unknownCode;

        var result = order.Adapt<OrderViewDto>(_config);

        Assert.Equal(unknownCode, result.CourtListTypeDescription);
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

    #region OrderDto -> JudicialAction Mapping Tests

    [Fact]
    public void OrderDto_To_JudicialAction_MapsDocumentData_WhenStatusIsApproved()
    {
        var expectedBytes = _faker.Random.Bytes(64);
        var orderDto = CreateOrderDto();
        orderDto.DocumentData = Convert.ToBase64String(expectedBytes);
        orderDto.SupportingDocumentData = Convert.ToBase64String(_faker.Random.Bytes(64));
        orderDto.Status = OrderStatus.Approved;

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.Equal(expectedBytes, result.Document);
    }

    [Fact]
    public void OrderDto_To_JudicialAction_DoesNotMapDocument_WhenStatusIsNotApproved()
    {
        var expectedBytes = _faker.Random.Bytes(64);
        var orderDto = CreateOrderDto();
        orderDto.DocumentData = Convert.ToBase64String(expectedBytes);
        orderDto.SupportingDocumentData = Convert.ToBase64String(_faker.Random.Bytes(64));
        orderDto.Status = OrderStatus.Unapproved;

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.Null(result.Document);
    }

    [Fact]
    public void OrderDto_To_JudicialAction_DoesNotMapDocument_WhenStatusIsAwaitingDocumentation()
    {
        var expectedBytes = _faker.Random.Bytes(64);
        var orderDto = CreateOrderDto();
        orderDto.DocumentData = Convert.ToBase64String(expectedBytes);
        orderDto.SupportingDocumentData = Convert.ToBase64String(_faker.Random.Bytes(64));
        orderDto.Status = OrderStatus.AwaitingDocumentation;

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.Null(result.Document);
    }

    [Fact]
    public void OrderDto_To_JudicialAction_FallsBackToSupportingDocumentData_WhenDocumentDataIsNull()
    {
        var expectedBytes = _faker.Random.Bytes(64);
        var orderDto = CreateOrderDto();
        orderDto.DocumentData = null;
        orderDto.SupportingDocumentData = Convert.ToBase64String(expectedBytes);
        orderDto.Status = OrderStatus.Approved;

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.Equal(expectedBytes, result.Document);
    }

    [Fact]
    public void OrderDto_To_JudicialAction_FallsBackToSupportingDocumentData_WhenDocumentDataIsEmpty()
    {
        var expectedBytes = _faker.Random.Bytes(64);
        var orderDto = CreateOrderDto();
        orderDto.DocumentData = string.Empty;
        orderDto.SupportingDocumentData = Convert.ToBase64String(expectedBytes);
        orderDto.Status = OrderStatus.Approved;

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.Equal(expectedBytes, result.Document);
    }

    [Fact]
    public void OrderDto_To_JudicialAction_Throws_WhenBothDocumentDataAndSupportingDocumentDataAreNullAndStatusIsApproved()
    {
        var orderDto = CreateOrderDto();
        orderDto.DocumentData = null;
        orderDto.SupportingDocumentData = null;
        orderDto.Status = OrderStatus.Approved;

        Assert.Throws<InvalidOperationException>(() => orderDto.Adapt<JudicialAction>(_config));
    }

    [Fact]
    public void OrderDto_To_JudicialAction_Throws_WhenDocumentDataIsInvalidBase64()
    {
        var orderDto = CreateOrderDto();
        orderDto.DocumentData = "not-valid-base64!!!";
        orderDto.Status = OrderStatus.Approved;

        Assert.Throws<InvalidOperationException>(() => orderDto.Adapt<JudicialAction>(_config));
    }

    [Fact]
    public void OrderDto_To_JudicialAction_Throws_WhenSupportingDocumentDataIsInvalidBase64()
    {
        var orderDto = CreateOrderDto();
        orderDto.DocumentData = null;
        orderDto.SupportingDocumentData = "not-valid-base64!!!";
        orderDto.Status = OrderStatus.Approved;

        Assert.Throws<InvalidOperationException>(() => orderDto.Adapt<JudicialAction>(_config));
    }

    [Fact]
    public void OrderDto_To_JudicialAction_MapsSignatureAppliedFromSigned()
    {
        var orderDto = CreateOrderDto();
        orderDto.Signed = true;

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.True(result.SignatureApplied);
    }

    [Fact]
    public void OrderDto_To_JudicialAction_MapsCommentFromComments()
    {
        var expectedComment = _faker.Lorem.Sentence();
        var orderDto = CreateOrderDto();
        orderDto.Comments = expectedComment;

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.Equal(expectedComment, result.Comment);
    }

    [Fact]
    public void OrderDto_To_JudicialAction_SetsEmptyOrderTerms()
    {
        var orderDto = CreateOrderDto();

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.NotNull(result.OrderTerms);
        Assert.Empty(result.OrderTerms);
    }

    [Theory]
    [InlineData(OrderStatus.Approved, "APPR")]
    [InlineData(OrderStatus.Unapproved, "NAPP")]
    [InlineData(OrderStatus.AwaitingDocumentation, "AFDC")]
    public void OrderDto_To_JudicialAction_MapsDecisionCode(OrderStatus status, string expectedCode)
    {
        var orderDto = CreateOrderDto();
        orderDto.Status = status;

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.Equal(expectedCode, result.DecisionCode);
    }

    [Fact]
    public void OrderDto_To_JudicialAction_SetsNullDecisionCode_ForUnmappedStatus()
    {
        var orderDto = CreateOrderDto();
        orderDto.Status = OrderStatus.Pending;

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.Null(result.DecisionCode);
    }

    [Fact]
    public void OrderDto_To_JudicialAction_SetsActionDateFromProcessedDate_WhenNotNull()
    {
        var expectedDate = _faker.Date.Recent();
        var orderDto = CreateOrderDto();
        orderDto.ProcessedDate = expectedDate;

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.Equal(expectedDate, result.ActionDate);
    }

    [Fact]
    public void OrderDto_To_JudicialAction_SetsDefaultActionDate_WhenProcessedDateIsNull()
    {
        var orderDto = CreateOrderDto();
        orderDto.ProcessedDate = null;

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.Equal(default, result.ActionDate);
    }

    [Fact]
    public void OrderDto_To_JudicialAction_SetsNullRejectedDate_WhenApproved()
    {
        var orderDto = CreateOrderDto();
        orderDto.Status = OrderStatus.Approved;
        orderDto.ProcessedDate = _faker.Date.Recent();

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.Null(result.RejectedDate);
    }

    [Fact]
    public void OrderDto_To_JudicialAction_SetsNullSignedDate_WhenNotSigned()
    {
        var orderDto = CreateOrderDto();
        orderDto.Signed = false;

        var result = orderDto.Adapt<JudicialAction>(_config);

        Assert.Null(result.SignedDate);
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
                CourtFileNo = _faker.Random.Replace("####-##"),
                CourtDivisionCd = _faker.PickRandom("R", "I"),
                CourtClassCd = _faker.PickRandom("A", "Y", "T", "F", "C", "M", "L"),
                PhysicalFileId = _faker.Random.Int(100, 9999),
                FullFileNo = _faker.Random.Replace("####-##"),
                Referral = new Referral
                {
                    SentToPartId = _faker.Random.Int(1, 1000),
                    SentToName = _faker.Name.FullName(),
                    ReferredDocumentId = _faker.Random.Int(1, 1000)
                },
                Parties = [],
                PackageDocuments = [],
                RelevantCeisDocuments = []
            },
            Status = _faker.PickRandom<OrderStatus>(),
            Signed = _faker.Random.Bool(),
            Comments = _faker.Lorem.Sentence(),
            DocumentData = _faker.Random.Bytes(64),
            SupportingDocumentData = _faker.Random.Bytes(64),
            ProcessedDate = _faker.Date.Recent(),
            Ent_Dtm = _faker.Date.Past(),
            Upd_Dtm = _faker.Date.Recent(),
            Ent_UserId = _faker.Random.AlphaNumeric(10),
            Upd_UserId = _faker.Random.AlphaNumeric(10),
            ReminderNotificationsSent = _faker.Random.Int(0, 10),
            ReassignmentNotificationsSent = _faker.Random.Int(0, 10)
        };
    }

    private OrderDto CreateOrderDto()
    {
        return new OrderDto
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            OrderRequest = new OrderRequestDto
            {
                CourtFileNo = _faker.Random.Replace("####-##"),
                CourtDivisionCd = _faker.PickRandom("R", "I"),
                CourtClassCd = _faker.PickRandom("A", "Y", "T", "F", "C", "M", "L"),
                PhysicalFileId = _faker.Random.Int(100, 9999),
                FullFileNo = _faker.Random.Replace("####-##"),
                Referral = new ReferralDto
                {
                    SentToPartId = _faker.Random.Int(1, 1000),
                    SentToName = _faker.Name.FullName(),
                    ReferredDocumentId = _faker.Random.Int(1, 1000)
                },
                Parties = [],
                PackageDocuments = [],
                RelevantCeisDocuments = []
            },
            Status = _faker.PickRandom<OrderStatus>(),
            Signed = _faker.Random.Bool(),
            Comments = _faker.Lorem.Sentence(),
            DocumentData = Convert.ToBase64String(_faker.Random.Bytes(64)),
            SupportingDocumentData = Convert.ToBase64String(_faker.Random.Bytes(64)),
            ProcessedDate = _faker.Date.Recent(),
            CreatedDate = _faker.Date.Past(),
            UpdatedDate = _faker.Date.Recent()
        };
    }

    #endregion
}

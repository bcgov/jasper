using Scv.Api.Infrastructure.Options;
using Scv.Api.Jobs;
using Scv.Models.Order;
using Xunit;

namespace tests.api.Jobs;

public class OrderNotificationFilterTests
{
    private static OrderNotificationFilter CreateFilter(
        string priorityTypes = null,
        string orderTypes = null,
        string courtClasses = null)
    {
        return new OrderNotificationFilter(new OrderNotificationOptions
        {
            PriorityTypes = priorityTypes,
            OrderTypes = orderTypes,
            CourtClasses = courtClasses
        });
    }

    private static OrderRequestDto CreateOrderRequest(
        string priorityType = PriorityTypeDescriptor.PROTECTION_ORDER_TYPE,
        string courtListTypeCd = CourtListTypeDescriptor.FAMILY_COURT_LIST_TYPE,
        string courtClassCd = "F") => new()
        {
            CourtClassCd = courtClassCd,
            Referral = new ReferralDto
            {
                PriorityType = priorityType,
                CourtListTypeCd = courtListTypeCd
            }
        };

    #region No filters configured

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",")]
    public void GetRejectedFilter_ReturnsNull_WhenFiltersAreUnsetOrEmpty(string value)
    {
        var filter = CreateFilter(value, value, value);

        Assert.Null(filter.GetRejectedFilter(CreateOrderRequest()));
    }

    [Fact]
    public void GetRejectedFilter_ReturnsNull_ForOrderWithNoReferralOrClass_WhenNoFiltersConfigured()
    {
        var filter = CreateFilter();

        Assert.Null(filter.GetRejectedFilter(new OrderRequestDto()));
        Assert.Null(filter.GetRejectedFilter(null));
    }

    #endregion

    #region Priority filter

    [Theory]
    [InlineData("PRO", "PRO")]
    [InlineData("PRO,CRTD,OTHR", "CRTD")]
    [InlineData("pro", "PRO")]
    [InlineData("PRO", "pro")]
    [InlineData(" PRO , CRTD ", "CRTD")]
    [InlineData("PRO;OTHR", "OTHR")]
    public void GetRejectedFilter_ReturnsNull_WhenPriorityTypeIsInFilter(string configured, string priorityType)
    {
        var filter = CreateFilter(priorityTypes: configured);

        Assert.Null(filter.GetRejectedFilter(CreateOrderRequest(priorityType: priorityType)));
    }

    [Theory]
    [InlineData("PRO", "CRTD")]
    [InlineData("PRO,CRTD", "OTHR")]
    [InlineData("PRO", "")]
    [InlineData("PRO", null)]
    [InlineData("PRO", "URG")]
    public void GetRejectedFilter_RejectsOnPriority_WhenPriorityTypeIsNotInFilter(string configured, string priorityType)
    {
        var filter = CreateFilter(priorityTypes: configured);

        Assert.Equal(
            OrderNotificationOptions.PriorityTypesKey,
            filter.GetRejectedFilter(CreateOrderRequest(priorityType: priorityType)));
    }

    [Fact]
    public void GetRejectedFilter_RejectsOnPriority_WhenReferralOrRequestIsNull()
    {
        var filter = CreateFilter(priorityTypes: "PRO");

        Assert.Equal(OrderNotificationOptions.PriorityTypesKey, filter.GetRejectedFilter(new OrderRequestDto { Referral = null }));
        Assert.Equal(OrderNotificationOptions.PriorityTypesKey, filter.GetRejectedFilter(null));
    }

    #endregion

    #region Order type filter

    [Theory]
    [InlineData("Order", "PSC")]
    [InlineData("Order", "PFA")]
    [InlineData("Desk Order", "PSM")]
    [InlineData("Desk Order", "PFM")]
    [InlineData("Order,Desk Order", "PSC")]
    [InlineData("Order,Desk Order", "PFM")]
    [InlineData("order", "PSC")]
    [InlineData("desk order", "PSM")]
    [InlineData(" Order ; Desk Order ", "PSM")]
    public void GetRejectedFilter_ReturnsNull_WhenOrderTypeMatches(string configured, string courtListTypeCd)
    {
        var filter = CreateFilter(orderTypes: configured);

        Assert.Null(filter.GetRejectedFilter(CreateOrderRequest(courtListTypeCd: courtListTypeCd)));
    }

    [Theory]
    [InlineData("Order", "PSM")]
    [InlineData("Order", "PFM")]
    [InlineData("Desk Order", "PSC")]
    [InlineData("Desk Order", "PFA")]
    [InlineData("Order", null)]
    [InlineData("Order", "")]
    [InlineData("Desk Order", "XYZ")]
    public void GetRejectedFilter_RejectsOnOrderType_WhenOrderTypeDoesNotMatch(string configured, string courtListTypeCd)
    {
        var filter = CreateFilter(orderTypes: configured);

        Assert.Equal(
            OrderNotificationOptions.OrderTypesKey,
            filter.GetRejectedFilter(CreateOrderRequest(courtListTypeCd: courtListTypeCd)));
    }

    [Fact]
    public void GetRejectedFilter_RejectsOnOrderType_WhenReferralIsNull()
    {
        var filter = CreateFilter(orderTypes: "Order");

        Assert.Equal(OrderNotificationOptions.OrderTypesKey, filter.GetRejectedFilter(new OrderRequestDto { Referral = null }));
    }

    #endregion

    #region Court class filter

    [Theory]
    [InlineData("F", "F")]
    [InlineData("C", "C")]
    [InlineData("F,C", "F")]
    [InlineData("F,C", "C")]
    [InlineData("f", "F")]
    [InlineData("F", "f")]
    [InlineData(" F ; C ", "C")]
    public void GetRejectedFilter_ReturnsNull_WhenCourtClassMatches(string configured, string courtClassCd)
    {
        var filter = CreateFilter(courtClasses: configured);

        Assert.Null(filter.GetRejectedFilter(CreateOrderRequest(courtClassCd: courtClassCd)));
    }

    [Theory]
    [InlineData("F", "C")]
    [InlineData("C", "F")]
    [InlineData("F,C", "M")]
    [InlineData("F,C", "L")]
    [InlineData("F,C", "A")]
    [InlineData("F", null)]
    [InlineData("F", "")]
    [InlineData("F", "INVALID")]
    public void GetRejectedFilter_RejectsOnCourtClass_WhenCourtClassDoesNotMatch(string configured, string courtClassCd)
    {
        var filter = CreateFilter(courtClasses: configured);

        Assert.Equal(
            OrderNotificationOptions.CourtClassesKey,
            filter.GetRejectedFilter(CreateOrderRequest(courtClassCd: courtClassCd)));
    }

    #endregion

    #region Combined filters

    [Fact]
    public void GetRejectedFilter_ReturnsNull_WhenAllFiltersMatch()
    {
        var filter = CreateFilter("PRO,CRTD", "Order", "F");

        Assert.Null(filter.GetRejectedFilter(CreateOrderRequest(
            priorityType: "CRTD",
            courtListTypeCd: CourtListTypeDescriptor.FAMILY_COURT_LIST_TYPE,
            courtClassCd: "F")));
    }

    [Theory]
    [InlineData("OTHR", "PSC", "F", OrderNotificationOptions.PriorityTypesKey)]
    [InlineData("PRO", "PSM", "F", OrderNotificationOptions.OrderTypesKey)]
    [InlineData("PRO", "PSC", "C", OrderNotificationOptions.CourtClassesKey)]
    public void GetRejectedFilter_RejectsWhenAnySingleFilterFails(
        string priorityType, string courtListTypeCd, string courtClassCd, string expectedRejection)
    {
        var filter = CreateFilter("PRO", "Order", "F");

        Assert.Equal(
            expectedRejection,
            filter.GetRejectedFilter(CreateOrderRequest(priorityType, courtListTypeCd, courtClassCd)));
    }

    [Fact]
    public void GetRejectedFilter_ReportsFirstFailingFilter_InPriorityTypeClassOrder()
    {
        var filter = CreateFilter("PRO", "Order", "F");

        // All three fail: priority is reported first.
        Assert.Equal(
            OrderNotificationOptions.PriorityTypesKey,
            filter.GetRejectedFilter(CreateOrderRequest("OTHR", "PSM", "C")));

        // Priority passes, order type and class fail: order type is reported first.
        Assert.Equal(
            OrderNotificationOptions.OrderTypesKey,
            filter.GetRejectedFilter(CreateOrderRequest("PRO", "PSM", "C")));
    }

    [Fact]
    public void GetRejectedFilter_OnlyAppliesFiltersThatAreConfigured()
    {
        var filter = CreateFilter(courtClasses: "F");

        Assert.Null(filter.GetRejectedFilter(CreateOrderRequest(priorityType: null, courtListTypeCd: null, courtClassCd: "F")));
    }

    #endregion
}

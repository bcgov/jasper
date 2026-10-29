using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
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
        var settings = new Dictionary<string, string>();
        if (priorityTypes != null) settings[OrderNotificationFilter.PriorityTypesKey] = priorityTypes;
        if (orderTypes != null) settings[OrderNotificationFilter.OrderTypesKey] = orderTypes;
        if (courtClasses != null) settings[OrderNotificationFilter.CourtClassesKey] = courtClasses;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new OrderNotificationFilter(configuration);
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
            OrderNotificationFilter.PriorityTypesKey,
            filter.GetRejectedFilter(CreateOrderRequest(priorityType: priorityType)));
    }

    [Fact]
    public void GetRejectedFilter_RejectsOnPriority_WhenReferralOrRequestIsNull()
    {
        var filter = CreateFilter(priorityTypes: "PRO");

        Assert.Equal(OrderNotificationFilter.PriorityTypesKey, filter.GetRejectedFilter(new OrderRequestDto { Referral = null }));
        Assert.Equal(OrderNotificationFilter.PriorityTypesKey, filter.GetRejectedFilter(null));
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
            OrderNotificationFilter.OrderTypesKey,
            filter.GetRejectedFilter(CreateOrderRequest(courtListTypeCd: courtListTypeCd)));
    }

    [Fact]
    public void GetRejectedFilter_RejectsOnOrderType_WhenReferralIsNull()
    {
        var filter = CreateFilter(orderTypes: "Order");

        Assert.Equal(OrderNotificationFilter.OrderTypesKey, filter.GetRejectedFilter(new OrderRequestDto { Referral = null }));
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
            OrderNotificationFilter.CourtClassesKey,
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
    [InlineData("OTHR", "PSC", "F", OrderNotificationFilter.PriorityTypesKey)]
    [InlineData("PRO", "PSM", "F", OrderNotificationFilter.OrderTypesKey)]
    [InlineData("PRO", "PSC", "C", OrderNotificationFilter.CourtClassesKey)]
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
            OrderNotificationFilter.PriorityTypesKey,
            filter.GetRejectedFilter(CreateOrderRequest("OTHR", "PSM", "C")));

        // Priority passes, order type and class fail: order type is reported first.
        Assert.Equal(
            OrderNotificationFilter.OrderTypesKey,
            filter.GetRejectedFilter(CreateOrderRequest("PRO", "PSM", "C")));
    }

    [Fact]
    public void GetRejectedFilter_OnlyAppliesFiltersThatAreConfigured()
    {
        var filter = CreateFilter(courtClasses: "F");

        Assert.Null(filter.GetRejectedFilter(CreateOrderRequest(priorityType: null, courtListTypeCd: null, courtClassCd: "F")));
    }

    #endregion

    #region GetUnrecognizedValues

    [Fact]
    public void GetUnrecognizedValues_ReturnsEmpty_WhenNothingConfigured()
    {
        Assert.Empty(CreateFilter().GetUnrecognizedValues());
    }

    [Fact]
    public void GetUnrecognizedValues_ReturnsEmpty_WhenAllValuesAreRecognized()
    {
        var filter = CreateFilter("PRO,crtd, OTHR", "Order,desk order", "F,c");

        Assert.Empty(filter.GetUnrecognizedValues());
    }

    [Fact]
    public void GetUnrecognizedValues_ReportsPriorityTypeNotInList()
    {
        var filter = CreateFilter(priorityTypes: "PRO,URG");

        Assert.Equal([$"{OrderNotificationFilter.PriorityTypesKey}=URG"], filter.GetUnrecognizedValues());
    }

    [Fact]
    public void GetUnrecognizedValues_ReportsUnknownOrderType()
    {
        var filter = CreateFilter(orderTypes: "Order,DeskOrder");

        Assert.Equal([$"{OrderNotificationFilter.OrderTypesKey}=DeskOrder"], filter.GetUnrecognizedValues());
    }

    [Theory]
    [InlineData("Criminal")]
    [InlineData("A")]
    [InlineData("M")]
    [InlineData("L")]
    public void GetUnrecognizedValues_ReportsCourtClassOtherThanFamilyOrCivil(string value)
    {
        var filter = CreateFilter(courtClasses: $"F,{value}");

        Assert.Equal([$"{OrderNotificationFilter.CourtClassesKey}={value}"], filter.GetUnrecognizedValues());
    }

    [Fact]
    public void GetUnrecognizedValues_ReportsValuesFromAllFilters()
    {
        var filter = CreateFilter("BAD1", "BAD2", "BAD3");

        Assert.Equal(
            [
                $"{OrderNotificationFilter.OrderTypesKey}=BAD2",
                $"{OrderNotificationFilter.CourtClassesKey}=BAD3",
                $"{OrderNotificationFilter.PriorityTypesKey}=BAD1"
            ],
            filter.GetUnrecognizedValues());
    }

    #endregion
}

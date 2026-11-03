using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;
using Xunit;

namespace tests.api.Jobs;

public class OrderNotificationOptionsTests
{
    private static List<string> Validate(
        string priorityTypes = null,
        string orderTypes = null,
        string courtClasses = null)
    {
        var options = new OrderNotificationOptions
        {
            PriorityTypes = priorityTypes,
            OrderTypes = orderTypes,
            CourtClasses = courtClasses
        };

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return [.. results.Select(r => r.ErrorMessage)];
    }

    private static IOptions<OrderNotificationOptions> BuildOptions(Dictionary<string, string> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<OrderNotificationOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations();

        return services.BuildServiceProvider().GetRequiredService<IOptions<OrderNotificationOptions>>();
    }

    #region Parsing

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",")]
    [InlineData(" , ; ")]
    public void Getters_ReturnEmpty_WhenUnsetOrBlank(string value)
    {
        var options = new OrderNotificationOptions { PriorityTypes = value, OrderTypes = value, CourtClasses = value };

        Assert.Empty(options.GetPriorityTypes());
        Assert.Empty(options.GetOrderTypes());
        Assert.Empty(options.GetCourtClasses());
    }

    [Theory]
    [InlineData("PRO,CRTD", new[] { "PRO", "CRTD" })]
    [InlineData("PRO;CRTD", new[] { "PRO", "CRTD" })]
    [InlineData(" PRO , CRTD ,, OTHR ", new[] { "PRO", "CRTD", "OTHR" })]
    [InlineData("PRO", new[] { "PRO" })]
    public void Getters_SplitOnCommaAndSemicolon_AndTrimEntries(string value, string[] expected)
    {
        var options = new OrderNotificationOptions { PriorityTypes = value, OrderTypes = value, CourtClasses = value };

        Assert.Equal(expected, options.GetPriorityTypes());
        Assert.Equal(expected, options.GetOrderTypes());
        Assert.Equal(expected, options.GetCourtClasses());
    }

    #endregion

    #region Binding

    [Fact]
    public void Bind_ReadsValuesFromNotifyKeys()
    {
        var options = BuildOptions(new()
        {
            [OrderNotificationOptions.PriorityTypesKey] = "PRO,CRTD",
            [OrderNotificationOptions.OrderTypesKey] = "Order",
            [OrderNotificationOptions.CourtClassesKey] = "F"
        }).Value;

        Assert.Equal(["PRO", "CRTD"], options.GetPriorityTypes());
        Assert.Equal(["Order"], options.GetOrderTypes());
        Assert.Equal(["F"], options.GetCourtClasses());
    }

    [Fact]
    public void Bind_LeavesFiltersEmpty_WhenKeysAreMissing()
    {
        var options = BuildOptions([]).Value;

        Assert.Empty(options.GetPriorityTypes());
        Assert.Empty(options.GetOrderTypes());
        Assert.Empty(options.GetCourtClasses());
    }

    [Fact]
    public void Bind_IgnoresUnrelatedKeys()
    {
        var options = BuildOptions(new() { ["SOMETHING_ELSE"] = "value" }).Value;

        Assert.Empty(options.GetPriorityTypes());
    }

    [Fact]
    public void Value_Throws_WhenBoundConfigurationIsInvalid()
    {
        var options = BuildOptions(new() { [OrderNotificationOptions.CourtClassesKey] = "Criminal" });

        var ex = Assert.Throws<OptionsValidationException>(() => options.Value);

        Assert.Contains($"Unrecognized {OrderNotificationOptions.CourtClassesKey} value: Criminal", ex.Message);
    }

    #endregion

    #region Validate

    [Fact]
    public void Validate_ReturnsNoErrors_WhenNothingConfigured()
    {
        Assert.Empty(Validate());
    }

    [Theory]
    [InlineData("PRO,crtd, OTHR", "Order,desk order", "F,c")]
    [InlineData("PRO;CRTD", "Order;Desk Order", "F;C")]
    [InlineData(",", ",", ",")]
    public void Validate_ReturnsNoErrors_WhenAllValuesAreRecognized(string priorityTypes, string orderTypes, string courtClasses)
    {
        Assert.Empty(Validate(priorityTypes, orderTypes, courtClasses));
    }

    [Fact]
    public void Validate_ReportsPriorityTypeNotInList()
    {
        Assert.Equal([$"Unrecognized {OrderNotificationOptions.PriorityTypesKey} value: URG"], Validate(priorityTypes: "PRO,URG"));
    }

    [Fact]
    public void Validate_ReportsUnknownOrderType()
    {
        Assert.Equal([$"Unrecognized {OrderNotificationOptions.OrderTypesKey} value: DeskOrder"], Validate(orderTypes: "Order,DeskOrder"));
    }

    [Theory]
    [InlineData("Criminal")]
    [InlineData("A")]
    [InlineData("M")]
    [InlineData("L")]
    public void Validate_ReportsCourtClassOtherThanFamilyOrCivil(string value)
    {
        Assert.Equal([$"Unrecognized {OrderNotificationOptions.CourtClassesKey} value: {value}"], Validate(courtClasses: $"F,{value}"));
    }

    [Fact]
    public void Validate_ReportsEachUnrecognizedValue()
    {
        Assert.Equal(
            [
                $"Unrecognized {OrderNotificationOptions.PriorityTypesKey} value: BAD1",
                $"Unrecognized {OrderNotificationOptions.PriorityTypesKey} value: BAD2"
            ],
            Validate(priorityTypes: "PRO,BAD1,BAD2"));
    }

    [Fact]
    public void Validate_ReportsValuesFromAllFilters()
    {
        Assert.Equal(
            [
                $"Unrecognized {OrderNotificationOptions.OrderTypesKey} value: BAD2",
                $"Unrecognized {OrderNotificationOptions.CourtClassesKey} value: BAD3",
                $"Unrecognized {OrderNotificationOptions.PriorityTypesKey} value: BAD1"
            ],
            Validate("BAD1", "BAD2", "BAD3"));
    }

    #endregion
}

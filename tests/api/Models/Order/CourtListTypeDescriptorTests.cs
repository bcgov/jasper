using Scv.Models.Order;
using Xunit;

namespace tests.api.Models.Order;

public class CourtListTypeDescriptorTests
{
    [Theory]
    [InlineData(CourtListTypeDescriptor.SMALL_CLAIMS_COURT_LIST_TYPE, CourtListTypeDescriptor.ORDER_DESCRIPTION)]
    [InlineData(CourtListTypeDescriptor.FAMILY_COURT_LIST_TYPE, CourtListTypeDescriptor.ORDER_DESCRIPTION)]
    [InlineData(CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_SMALL_CLAIMS_TYPE, CourtListTypeDescriptor.DESK_ORDER_DESCRIPTION)]
    [InlineData(CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_FAMILY_LIST_TYPE, CourtListTypeDescriptor.DESK_ORDER_DESCRIPTION)]
    [InlineData("XYZ", "XYZ")]
    [InlineData(null, null)]
    public void Describe_MapsCodesToDescriptions_AndPassesUnknownValuesThrough(string code, string expected)
    {
        Assert.Equal(expected, CourtListTypeDescriptor.Describe(code));
    }

    [Theory]
    [InlineData(CourtListTypeDescriptor.SMALL_CLAIMS_COURT_LIST_TYPE, true)]
    [InlineData(CourtListTypeDescriptor.FAMILY_COURT_LIST_TYPE, true)]
    [InlineData(CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_SMALL_CLAIMS_TYPE, false)]
    [InlineData(CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_FAMILY_LIST_TYPE, false)]
    [InlineData("XYZ", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData(CourtListTypeDescriptor.ORDER_DESCRIPTION, false)]
    [InlineData("psc", false)]
    public void IsOrder_OnlyMatchesOrderCodes(string value, bool expected)
    {
        Assert.Equal(expected, CourtListTypeDescriptor.IsOrder(value));
    }

    [Theory]
    [InlineData(CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_SMALL_CLAIMS_TYPE, true)]
    [InlineData(CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_FAMILY_LIST_TYPE, true)]
    [InlineData(CourtListTypeDescriptor.SMALL_CLAIMS_COURT_LIST_TYPE, false)]
    [InlineData(CourtListTypeDescriptor.FAMILY_COURT_LIST_TYPE, false)]
    [InlineData("XYZ", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData(CourtListTypeDescriptor.DESK_ORDER_DESCRIPTION, false)]
    [InlineData("psm", false)]
    public void IsDeskOrder_OnlyMatchesDeskOrderCodes(string value, bool expected)
    {
        Assert.Equal(expected, CourtListTypeDescriptor.IsDeskOrder(value));
    }
}

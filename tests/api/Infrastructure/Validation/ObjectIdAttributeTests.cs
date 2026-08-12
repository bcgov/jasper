using MongoDB.Bson;
using Scv.Api.Infrastructure.Validation;
using Xunit;

namespace tests.api.Infrastructure.Validation;

public class ObjectIdAttributeTests
{
    private readonly ObjectIdAttribute _attribute = new();

    [Fact]
    public void IsValid_ReturnsTrue_ForWellFormedObjectId()
    {
        var id = ObjectId.GenerateNewId().ToString();

        Assert.True(_attribute.IsValid(id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-object-id")]
    [InlineData("123")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData("507f1f77bcf86cd79943901")]
    [InlineData("507f1f77bcf86cd7994390111")]
    public void IsValid_ReturnsFalse_ForMalformedId(string id)
    {
        Assert.False(_attribute.IsValid(id));
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForNull()
    {
        Assert.False(_attribute.IsValid(null));
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForNonStringValue()
    {
        Assert.False(_attribute.IsValid(12345));
    }

    [Fact]
    public void DefaultErrorMessage_IsInvalidId()
    {
        Assert.Equal("Invalid id.", _attribute.FormatErrorMessage("id"));
    }
}

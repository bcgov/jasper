using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Models.Order;
using Scv.Api.Repositories;
using tests.api.Services;
using Xunit;

namespace tests.api.Repositories;

public class CsoClientTests : ServiceTestBase
{
    private static OrderActionDto CreateOrderAction() => new()
    {
        ReferredDocumentId = 1,
        DigitalSignatureApplied = true,
        JudicialDecisionCd = nameof(JudicialDecisionCd.APPR)
    };

    [Fact]
    public async Task SendOrderAsync_ReturnsTrue_WhenResponseIsSuccess()
    {
        var options = Options.Create(new CsoOptions { BaseUrl = "https://example.test", ActionUri = "/MockOrders/action" });
        SetupMockResponse(HttpStatusCode.OK, new { });

        var client = new CsoClient(HttpClient, options, NullLogger<CsoClient>.Instance);

        var result = await client.SendOrderAsync(CreateOrderAction());

        Assert.True(result);
        VerifyHttpRequest(HttpMethod.Post, "MockOrders/action");
    }

    [Fact]
    public async Task SendOrderAsync_ReturnsFalse_WhenResponseIsFailure()
    {
        var options = Options.Create(new CsoOptions { BaseUrl = "https://example.test", ActionUri = "/MockOrders/action" });
        SetupMockResponse(HttpStatusCode.BadRequest, new { });

        var client = new CsoClient(HttpClient, options, NullLogger<CsoClient>.Instance);

        var result = await client.SendOrderAsync(CreateOrderAction());

        Assert.False(result);
        VerifyHttpRequest(HttpMethod.Post, "MockOrders/action");
    }

    [Fact]
    public async Task SendOrderAsync_Throws_WhenOrderIsNull()
    {
        var options = Options.Create(new CsoOptions { BaseUrl = "https://example.test", ActionUri = "/MockOrders/action" });
        var client = new CsoClient(HttpClient, options, NullLogger<CsoClient>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendOrderAsync(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SendOrderAsync_Throws_WhenActionUriIsMissing(string actionUri)
    {
        var options = Options.Create(new CsoOptions { BaseUrl = "https://example.test", ActionUri = actionUri });
        var client = new CsoClient(HttpClient, options, NullLogger<CsoClient>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendOrderAsync(CreateOrderAction()));
    }
}

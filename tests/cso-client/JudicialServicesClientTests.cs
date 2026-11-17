using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CSOCommon.Clients.JudicialServices;
using CSOCommon.Models;
using Xunit;

namespace tests.cso_client;

public class JudicialServicesClientTests
{
    [Fact]
    public async Task SaveJudicialActionAsync_PreservesThreeMillisecondDigits()
    {
        var handler = new CapturingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var client = new JudicialServicesClient(httpClient)
        {
            BaseUrl = "https://localhost/"
        };
        var timestamp = new DateTime(2026, 11, 16, 22, 43, 17, 180, DateTimeKind.Utc);

        await client.SaveJudicialActionAsync(Guid.NewGuid(), 123, new JudicialAction
        {
            ActionDate = timestamp,
            RejectedDate = timestamp,
            SignedDate = timestamp
        });

        using var payload = JsonDocument.Parse(handler.RequestBody);
        var json = payload.RootElement;

        Assert.Equal("2026-11-16T22:43:17.180Z", json.GetProperty("actionDate").GetString());
        Assert.Equal("2026-11-16T22:43:17.180Z", json.GetProperty("rejectedDate").GetString());
        Assert.Equal("2026-11-16T22:43:17.180Z", json.GetProperty("signedDate").GetString());
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        public string RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }
}
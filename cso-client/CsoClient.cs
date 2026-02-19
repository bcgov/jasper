using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Models.Order;

namespace Scv.Api.Repositories;

public interface ICsoClient
{
    Task<bool> SendOrderAsync(OrderActionDto order, CancellationToken cancellationToken = default);
}

public class CsoClient : ICsoClient
{
    private static readonly JsonSerializerSettings SnakeCaseSerializerSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly HttpClient _httpClient;
    private readonly CsoOptions _options;
    private readonly ILogger<CsoClient> _logger;

    public CsoClient(HttpClient httpClient, IOptions<CsoOptions> options, ILogger<CsoClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendOrderAsync(OrderActionDto order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var actionUri = _options.ActionUri?.TrimStart('/') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(actionUri))
        {
            throw new InvalidOperationException("CSO ActionUri is not configured.");
        }

        var json = JsonConvert.SerializeObject(order, SnakeCaseSerializerSettings);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(actionUri, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = response.Content == null
                ? null
                : await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogWarning(
                "CSO order submit failed with status {StatusCode} {ReasonPhrase}.",
                (int)response.StatusCode,
                response.ReasonPhrase);
        }

        return response.IsSuccessStatusCode;
    }
}

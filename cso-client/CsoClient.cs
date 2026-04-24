using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Scv.Cso.Infrastructure.Options;
using Scv.Models.Order;

namespace Scv.Cso;

// This will be replaced by the JudicialServicesClient in the future
// while team is still waiting for the details from the PROD instance of Keycloak.
public interface ICsoClient
{
    Task<bool> SendOrderAsync(OrderActionDto order, CancellationToken cancellationToken = default);
}

public class CsoClient(HttpClient httpClient, IOptions<CsoOptions> options, ILogger<CsoClient> logger) : ICsoClient
{
    private static readonly JsonSerializerSettings SnakeCaseSerializerSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly CsoOptions _options = options.Value;
    private readonly ILogger<CsoClient> _logger = logger;

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
            var truncatedBody = body is { Length: > 500 }
                ? body[..500]
                : body;

            _logger.LogWarning(
                "CSO order submit failed with status {StatusCode} {ReasonPhrase} {Body}.",
                (int)response.StatusCode,
                response.ReasonPhrase,
                truncatedBody);
        }

        return response.IsSuccessStatusCode;
    }
}

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Scv.Api.Helpers.ContractResolver;

namespace Scv.Api.Services;

public interface ILambdaInvokerService
{
    Task<TResponse> InvokeAsync<TRequest, TResponse>(
        TRequest request,
        string functionName,
        TimeSpan? timeout = null)
        where TRequest : class
        where TResponse : class;
}

public class LambdaInvokerService(
    IAmazonLambda lambdaClient,
    ILogger<LambdaInvokerService> logger) : ILambdaInvokerService
{
    private readonly IAmazonLambda _lambdaClient = lambdaClient;
    private readonly ILogger<LambdaInvokerService> _logger = logger;

    private readonly JsonSerializerSettings _serializerSettings = new()
    {
        ContractResolver = new SafeContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
    };

    public async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        TRequest request,
        string functionName,
        TimeSpan? timeout = null)
        where TRequest : class
        where TResponse : class
    {
        if (string.IsNullOrWhiteSpace(functionName))
        {
            throw new InvalidOperationException("Lambda function name is not configured.");
        }

        using var cts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : null;

        try
        {
            _logger.LogInformation("Invoking Lambda function: {FunctionName} with timeout: {Timeout}",
                functionName, timeout?.TotalSeconds.ToString() ?? "default");

            var payload = JsonConvert.SerializeObject(request, _serializerSettings);

            var invokeRequest = new InvokeRequest
            {
                FunctionName = functionName,
                InvocationType = InvocationType.RequestResponse,
                Payload = payload,
            };

            var response = await _lambdaClient.InvokeAsync(invokeRequest, cts?.Token ?? default);

            if (response.FunctionError != null)
            {
                var errorPayload = Encoding.UTF8.GetString(response.Payload.ToArray());
                _logger.LogError("Lambda function error: {FunctionError}. Payload: {ErrorPayload}",
                    response.FunctionError, errorPayload);
                throw new InvalidOperationException($"Lambda function returned error: {response.FunctionError}");
            }

            if (response.StatusCode != 200)
            {
                _logger.LogError("Lambda invocation failed with status code: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException($"Lambda invocation failed with status code: {response.StatusCode}");
            }

            var responsePayload = Encoding.UTF8.GetString(response.Payload.ToArray());
            _logger.LogInformation("Lambda function invoked successfully. Response length: {Length}",
                responsePayload.Length);

            _logger.LogDebug("Lambda response payload: {Payload}", responsePayload);

            return JsonConvert.DeserializeObject<TResponse>(responsePayload, _serializerSettings);
        }
        catch (OperationCanceledException ex) when (cts?.IsCancellationRequested == true)
        {
            _logger.LogError(ex, "Lambda function invocation timed out after {Timeout} seconds for function: {FunctionName}",
                timeout.Value.TotalSeconds, functionName);
            throw new TimeoutException($"Lambda function '{functionName}' timed out after {timeout.Value.TotalSeconds} seconds", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for Lambda function: {FunctionName}", functionName);
            throw new InvalidOperationException($"Failed to deserialize response from Lambda function '{functionName}'", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking Lambda function: {FunctionName}", functionName);
            throw new InvalidOperationException($"Failed to invoke Lambda function '{functionName}'", ex);
        }
    }
}
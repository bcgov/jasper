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
        CancellationToken cancellationToken = default)
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
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        if (string.IsNullOrWhiteSpace(functionName))
        {
            throw new InvalidOperationException("Lambda function name is not configured.");
        }

        try
        {
            _logger.LogInformation("Invoking Lambda function: {FunctionName}", functionName);

            var payload = JsonConvert.SerializeObject(request, _serializerSettings);

            var invokeRequest = new InvokeRequest
            {
                FunctionName = functionName,
                InvocationType = InvocationType.RequestResponse,
                Payload = payload
            };

            var response = await _lambdaClient.InvokeAsync(invokeRequest, cancellationToken);

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
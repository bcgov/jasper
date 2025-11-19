using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Bogus;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Services;

public class LambdaInvokerServiceTests
{
    private readonly Mock<IAmazonLambda> _mockLambdaClient;
    private readonly Mock<ILogger<LambdaInvokerService>> _mockLogger;
    private readonly LambdaInvokerService _service;
    private readonly Faker _faker;

    public LambdaInvokerServiceTests()
    {
        _mockLambdaClient = new Mock<IAmazonLambda>();
        _mockLogger = new Mock<ILogger<LambdaInvokerService>>();
        _service = new LambdaInvokerService(_mockLambdaClient.Object, _mockLogger.Object);
        _faker = new Faker();
    }

    private class TestRequest
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public string Description { get; set; }
    }

    private class TestResponse
    {
        public string Result { get; set; }
        public bool Success { get; set; }
        public int Code { get; set; }
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnDeserializedResponse_WhenInvocationSucceeds()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest
        {
            Name = _faker.Person.FullName,
            Value = _faker.Random.Int(1, 100),
            Description = _faker.Lorem.Sentence()
        };
        var expectedResponse = new TestResponse
        {
            Result = _faker.Lorem.Word(),
            Success = true,
            Code = 200
        };
        var responsePayload = JsonConvert.SerializeObject(expectedResponse);
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responsePayload));

        var invokeResponse = new InvokeResponse
        {
            StatusCode = 200,
            FunctionError = null,
            Payload = responseStream
        };

        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invokeResponse);

        var result = await _service.InvokeAsync<TestRequest, TestResponse>(
            request,
            functionName);

        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Result, result.Result);
        Assert.Equal(expectedResponse.Success, result.Success);
        Assert.Equal(expectedResponse.Code, result.Code);

        _mockLambdaClient.Verify(
            x => x.InvokeAsync(
                It.Is<InvokeRequest>(r =>
                    r.FunctionName == functionName &&
                    r.InvocationType == InvocationType.RequestResponse),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldThrowInvalidOperationException_WhenFunctionNameIsNull()
    {
        var request = new TestRequest { Name = _faker.Person.FullName };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InvokeAsync<TestRequest, TestResponse>(request, null));

        Assert.Equal("Lambda function name is not configured.", exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_ShouldThrowInvalidOperationException_WhenFunctionNameIsEmpty()
    {
        var request = new TestRequest { Name = _faker.Person.FullName };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InvokeAsync<TestRequest, TestResponse>(request, string.Empty));

        Assert.Equal("Lambda function name is not configured.", exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_ShouldThrowInvalidOperationException_WhenFunctionNameIsWhitespace()
    {
        var request = new TestRequest { Name = _faker.Person.FullName };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InvokeAsync<TestRequest, TestResponse>(request, "   "));

        Assert.Equal("Lambda function name is not configured.", exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_ShouldThrowInvalidOperationException_WhenFunctionReturnsError()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest { Name = _faker.Person.FullName };
        var errorMessage = _faker.Lorem.Sentence();
        var errorPayload = JsonConvert.SerializeObject(new { errorMessage });
        var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorPayload));

        var invokeResponse = new InvokeResponse
        {
            StatusCode = 200,
            FunctionError = "Unhandled",
            Payload = errorStream
        };

        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invokeResponse);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InvokeAsync<TestRequest, TestResponse>(request, functionName));

        Assert.Contains($"Failed to invoke Lambda function '{functionName}'", exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_ShouldThrowInvalidOperationException_WhenStatusCodeIsNot200()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest { Name = _faker.Person.FullName };
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

        var invokeResponse = new InvokeResponse
        {
            StatusCode = 500,
            FunctionError = null,
            Payload = responseStream
        };

        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invokeResponse);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InvokeAsync<TestRequest, TestResponse>(request, functionName));

        Assert.Contains($"Failed to invoke Lambda function '{functionName}'", exception.Message);
    }

    [Fact]
    public async Task InvokeAsync_ShouldThrowInvalidOperationException_WhenResponseIsInvalidJson()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest { Name = _faker.Person.FullName };
        var invalidJson = "{ invalid json }";
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));

        var invokeResponse = new InvokeResponse
        {
            StatusCode = 200,
            FunctionError = null,
            Payload = responseStream
        };

        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invokeResponse);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InvokeAsync<TestRequest, TestResponse>(request, functionName));
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassTimeout_WhenProvided()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest { Name = _faker.Person.FullName };
        var timeout = TimeSpan.FromMinutes(5);
        var responsePayload = JsonConvert.SerializeObject(new TestResponse { Success = true });
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responsePayload));

        var invokeResponse = new InvokeResponse
        {
            StatusCode = 200,
            FunctionError = null,
            Payload = responseStream
        };

        var cts = new CancellationTokenSource(timeout);
        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invokeResponse);

        await _service.InvokeAsync<TestRequest, TestResponse>(
            request,
            functionName,
            timeout);

        _mockLambdaClient.Verify(
            x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldSerializeRequestWithNullValueHandling()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest
        {
            Name = _faker.Person.FullName,
            Value = _faker.Random.Int(1, 100),
            Description = null
        };
        var responsePayload = JsonConvert.SerializeObject(new TestResponse { Success = true });
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responsePayload));

        var invokeResponse = new InvokeResponse
        {
            StatusCode = 200,
            FunctionError = null,
            Payload = responseStream
        };

        string capturedPayload = null;
        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InvokeRequest, CancellationToken>((req, ct) => capturedPayload = req.Payload)
            .ReturnsAsync(invokeResponse);

        await _service.InvokeAsync<TestRequest, TestResponse>(request, functionName);

        Assert.NotNull(capturedPayload);
        Assert.DoesNotContain("\"Description\"", capturedPayload);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogInformation_WhenInvokingFunction()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest { Name = _faker.Person.FullName };
        var responsePayload = JsonConvert.SerializeObject(new TestResponse { Success = true });
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responsePayload));

        var invokeResponse = new InvokeResponse
        {
            StatusCode = 200,
            FunctionError = null,
            Payload = responseStream
        };

        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invokeResponse);

        await _service.InvokeAsync<TestRequest, TestResponse>(request, functionName);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Invoking Lambda function: {functionName}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogError_WhenFunctionReturnsError()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest { Name = _faker.Person.FullName };
        var errorPayload = JsonConvert.SerializeObject(new { error = "Test error" });
        var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorPayload));

        var invokeResponse = new InvokeResponse
        {
            StatusCode = 200,
            FunctionError = "Unhandled",
            Payload = errorStream
        };

        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invokeResponse);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InvokeAsync<TestRequest, TestResponse>(request, functionName));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Lambda function error")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogError_WhenStatusCodeIsNot200()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest { Name = _faker.Person.FullName };
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

        var invokeResponse = new InvokeResponse
        {
            StatusCode = 404,
            FunctionError = null,
            Payload = responseStream
        };

        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invokeResponse);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InvokeAsync<TestRequest, TestResponse>(request, functionName));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Lambda invocation failed with status code: 404")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogError_WhenExceptionOccurs()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest { Name = _faker.Person.FullName };
        var expectedException = new Exception("Test exception");

        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InvokeAsync<TestRequest, TestResponse>(request, functionName));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString().Contains("Error invoking Lambda function") &&
                    v.ToString().Contains(functionName)),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleComplexRequestObject()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest
        {
            Name = _faker.Person.FullName,
            Value = _faker.Random.Int(1, 1000),
            Description = _faker.Lorem.Paragraph()
        };
        var expectedResponse = new TestResponse
        {
            Result = _faker.Lorem.Word(),
            Success = true,
            Code = 200
        };
        var responsePayload = JsonConvert.SerializeObject(expectedResponse);
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responsePayload));

        var invokeResponse = new InvokeResponse
        {
            StatusCode = 200,
            FunctionError = null,
            Payload = responseStream
        };

        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invokeResponse);

        var result = await _service.InvokeAsync<TestRequest, TestResponse>(
            request,
            functionName);

        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Result, result.Result);
        Assert.Equal(expectedResponse.Success, result.Success);
        Assert.Equal(expectedResponse.Code, result.Code);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleEmptyResponsePayload()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest { Name = _faker.Person.FullName };
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes("null"));

        var invokeResponse = new InvokeResponse
        {
            StatusCode = 200,
            FunctionError = null,
            Payload = responseStream
        };

        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invokeResponse);

        var result = await _service.InvokeAsync<TestRequest, TestResponse>(
            request,
            functionName);

        Assert.Null(result);
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetCorrectInvocationType()
    {
        var functionName = _faker.Lorem.Word();
        var request = new TestRequest { Name = _faker.Person.FullName };
        var responsePayload = JsonConvert.SerializeObject(new TestResponse { Success = true });
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responsePayload));

        var invokeResponse = new InvokeResponse
        {
            StatusCode = 200,
            FunctionError = null,
            Payload = responseStream
        };

        InvokeRequest capturedRequest = null;
        _mockLambdaClient
            .Setup(x => x.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InvokeRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(invokeResponse);

        await _service.InvokeAsync<TestRequest, TestResponse>(request, functionName);

        Assert.NotNull(capturedRequest);
        Assert.Equal(InvocationType.RequestResponse, capturedRequest.InvocationType);
        Assert.Equal(functionName, capturedRequest.FunctionName);
    }
}
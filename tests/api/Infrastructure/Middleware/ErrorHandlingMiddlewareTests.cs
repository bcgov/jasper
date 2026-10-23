using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Scv.Api.Infrastructure.Middleware;
using Scv.Core.Exceptions;
using Xunit;

namespace tests.api.Infrastructure.Middleware;

public class ErrorHandlingMiddlewareTests
{
    [Theory]
    [InlineData(typeof(UnsupportedDocumentFormatException), StatusCodes.Status415UnsupportedMediaType, DocumentConversionErrorCodes.FormatUnsupported)]
    [InlineData(typeof(UnprocessableDocumentException), StatusCodes.Status422UnprocessableEntity, DocumentConversionErrorCodes.ConversionFailed)]
    [InlineData(typeof(DocumentSizeLimitExceededException), StatusCodes.Status413PayloadTooLarge, DocumentConversionErrorCodes.SizeLimitExceeded)]
    [InlineData(typeof(DocumentConversionTimeoutException), StatusCodes.Status504GatewayTimeout, DocumentConversionErrorCodes.ConversionTimeout)]
    public async Task Invoke_MapsConversionExceptionsToExpectedHttpStatus(
        Type exceptionType,
        int expectedStatus,
        string expectedCode)
    {
        const string message = "This document format could not be converted for viewing.";
        var exception = (Exception)Activator.CreateInstance(exceptionType, message);
        var middleware = new ErrorHandlingMiddleware(
            _ => Task.FromException(exception),
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<ILogger<ErrorHandlingMiddleware>>(),
            Options.Create(new JsonOptions()));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.Invoke(context, null);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(message, response.RootElement.GetProperty("error").GetString());
        Assert.Equal(expectedCode, response.RootElement.GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, response.RootElement.GetProperty("type").ValueKind);
        Assert.Equal(JsonValueKind.Null, response.RootElement.GetProperty("details").ValueKind);
        Assert.Equal(JsonValueKind.Null, response.RootElement.GetProperty("stackTrace").ValueKind);
    }

    [Fact]
    public async Task Invoke_MapsUnexpectedConversionFailureTo500()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => Task.FromException(new Exception("unexpected converter failure")),
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<ILogger<ErrorHandlingMiddleware>>(),
            Options.Create(new JsonOptions()));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.Invoke(context, null);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }
}

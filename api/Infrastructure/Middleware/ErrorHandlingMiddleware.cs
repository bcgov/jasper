using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scv.Core.Helpers.Exceptions;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;
using Scv.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Scv.Api.Infrastructure.Middleware
{
    /// <summary>
    /// ErrorHandlingMiddleware class, provides a way to catch and handle unhandled errors in a generic way.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        #region Variables

        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly JsonOptions _options;

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Creates a new instance of an ErrorHandlingMiddleware class, and initializes it with the specified arguments.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="env"></param>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public ErrorHandlingMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<ErrorHandlingMiddleware> logger, IOptions<JsonOptions> options)
        {
            _next = next;
            _env = env;
            _logger = logger;
            _options = options.Value;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Handle the exception if one occurs. Note this wont catch exceptions created from async Task functions.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context, ScvDbContext db)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
            finally
            {
                await AuditLog(context, db);
            }
        }

        private async Task AuditLog(HttpContext context, ScvDbContext db)
        {
            // May change this later, to include requests that aren't logged into keycloak.
            if (!string.IsNullOrEmpty(context.User.PreferredUsername()))
            {
                var request = context.Request;
                var jsonBody = "{}";
                if (request.ContentType != null && request.ContentType.Contains("json"))
                {
                    request.Body.Position = 0;
                    using var reader = new StreamReader(request.Body);
                    jsonBody = await reader.ReadToEndAsync();
                    request.Body.Position = 0;
                }
                var audit = new Audit
                {
                    Created = DateTimeOffset.UtcNow,
                    Path = $"{request.Method} {request.GetEncodedPathAndQuery()}",
                    Action = $"{(request.RouteValues.ContainsKey("action") ? request.RouteValues["action"] : "")}",
                    JsonBody = jsonBody,
                    IpAddress = request.Headers.ContainsKey("X-Real-IP") ? request.Headers["X-Real-IP"].ToString() : "",
                    ResponseCode = context.Response?.StatusCode.ToString(),
                    UserId = context.User.PreferredUsername()
                };
                await db.AddAsync(audit);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Handle the exception by returning an appropriate error message depending on type and environment.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var code = HttpStatusCode.InternalServerError;
            var message = "An unhandled error has occurred.";

            switch (ex)
            {
                case SecurityTokenException _:
                    code = HttpStatusCode.Unauthorized;
                    message = "The authentication token is invalid.";
                    break;

                case KeyNotFoundException _:
                    code = HttpStatusCode.BadRequest;
                    message = "Item does not exist.";
                    _logger.LogDebug(ex, "Middleware caught unhandled exception.");
                    break;

                case NotAuthorizedException _:
                    code = HttpStatusCode.Forbidden;
                    message = "User is not authorized to perform this action.";
                    _logger.LogWarning(ex, ex.Message);
                    break;

                case ConfigurationException _:
                    code = HttpStatusCode.InternalServerError;
                    message = "Application configuration details invalid or missing.";
                    _logger.LogError(ex, ex.Message);
                    break;

                case NotFoundException _:
                    code = HttpStatusCode.NotFound;
                    message = ex.Message;
                    break;

                case BadRequestException _:
                case InvalidOperationException _:
                    code = HttpStatusCode.BadRequest;
                    message = ex.Message;
                    break;

                case JCCommon.Clients.FileServices.ApiException exception:
                    code = HttpStatusCode.InternalServerError;
                    message = exception.Message;
                    _logger.LogError(ex, ex.Message);
                    break;

                case JCCommon.Clients.LocationServices.ApiException exception:
                    code = HttpStatusCode.InternalServerError;
                    message = exception.Message;
                    _logger.LogError(ex, ex.Message);
                    break;

                case JCCommon.Clients.LookupCodeServices.ApiException exception:
                    code = HttpStatusCode.InternalServerError;
                    message = exception.Message;
                    _logger.LogError(ex, ex.Message);
                    break;

                default:
                    _logger.LogError(ex, "Middleware caught unhandled exception.");
                    break;
            }

            if (!context.Response.HasStarted)
            {
                var result = JsonSerializer.Serialize(new ErrorResponseModel(_env, ex, message), _options.JsonSerializerOptions);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)code;
                await context.Response.WriteAsync(result);
            }
            else
            {
                // Had to do this because odd errors were occurring when bearer tokens were failing.
                await context.Response.WriteAsync(string.Empty);
            }
        }

        #endregion Methods
    }
}
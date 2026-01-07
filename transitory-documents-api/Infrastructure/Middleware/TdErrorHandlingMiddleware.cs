
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scv.Core.Helpers.Exceptions;
using Scv.Models;
using System.Net;
using System.Text.Json;

namespace Scv.TdApi.Infrastructure.Middleware
{
    /// <summary>
    /// ErrorHandlingMiddleware class, provides a way to catch and handle unhandled errors in a generic way.
    /// </summary>
    public class TdErrorHandlingMiddleware
    {
        #region Variables

        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<TdErrorHandlingMiddleware> _logger;
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
        public TdErrorHandlingMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<TdErrorHandlingMiddleware> logger, IOptions<JsonOptions> options)
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
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
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

                case FileNotFoundException _:
                case DirectoryNotFoundException _:
                    code = HttpStatusCode.NotFound;
                    message = "The requested file or directory was not found.";
                    _logger.LogDebug(ex, "File or directory not found: {Message}", ex.Message);
                    break;

                case IOException _:
                    code = HttpStatusCode.InternalServerError;
                    message = "Unable to access the file system. Please try again later.";
                    _logger.LogError(ex, "File system access error: {Message}", ex.Message);
                    break;

                case BadRequestException _:
                case InvalidOperationException _:
                    code = HttpStatusCode.BadRequest;
                    message = ex.Message;
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
using System;
using JCCommon.Framework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using tests.api.Controllers;
using BasicAuthenticationHeaderValue = JCCommon.Framework.BasicAuthenticationHeaderValue;
using Scv.Core.Helpers.Extensions;

namespace tests.api.Helpers
{
    /// <summary>
    /// This builds out our environment before injecting into controllers.
    /// </summary>
    public class EnvironmentBuilder
    {
        public ILoggerFactory LogFactory;
        public HttpClient HttpClient;
        public IConfiguration Configuration;

        public EnvironmentBuilder(string usernameKey, string passwordKey, string urlKey)
        {
            //Load in secrets, this uses the secrets file as the API.
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: true);
            builder.AddUserSecrets<FilesControllerTests>();
            Configuration = builder.Build();

            //Create HTTP client, usually done by Startup.cs - which handles the life cycle of HttpClient nicely.
            HttpClient = new HttpClient();

            HttpClient.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(
                Configuration.GetNonEmptyValue(usernameKey),
                Configuration.GetNonEmptyValue(passwordKey));

            HttpClient.BaseAddress = new Uri(Configuration.GetNonEmptyValue(urlKey).EnsureEndingForwardSlash());
            //Create logger.
            LogFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                    .AddConsole();
            });
        }
    }
}
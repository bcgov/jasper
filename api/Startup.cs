using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ColeSoft.Extensions.Logging.Splunk;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Console;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Scv.Api.Hubs;
using Scv.Api.Infrastructure;
using Scv.Api.Infrastructure.Authentication;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Infrastructure.Encryption;
using Scv.Api.Infrastructure.Handler;
using Scv.Api.Infrastructure.HealthChecks;
using Scv.Api.Infrastructure.Middleware;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Services.EF;
using Scv.Api.SignalR;
using Scv.Core.ContractResolver;
using Scv.Core.Helpers;
using Scv.Core.Helpers.Extensions;
using Scv.Cso.Infrastructure.Options;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models;

namespace Scv.Api
{
    public class Startup(IWebHostEnvironment env, IConfiguration configuration)
    {
        private IWebHostEnvironment CurrentEnvironment { get; } = env;

        private IConfiguration Configuration { get; } = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddExceptionHandler<CustomExceptionHandler>();
            services.AddProblemDetails();

            services.AddOptions<CsoOptions>()
                .Bind(Configuration.GetSection("CSO"))
                .ValidateDataAnnotations();

            services.Configure<JobsSubmitOrderOptions>(options =>
            {
                var retryCountRaw = Configuration["JOBS:SubmitOrder:RetryCount"];
                options.RetryCount = int.TryParse(retryCountRaw, out var retryCount)
                    ? retryCount
                    : new JobsSubmitOrderOptions().RetryCount;
            });
            services.Configure<JobsFailureEmailOptions>(Configuration.GetSection("JOBS:FailureEmail"));
            services.Configure<JobsRetrySubmitOrderOptions>(Configuration.GetSection("JOBS:RetrySubmitOrder"));
            services.Configure<JobsRetryUrgentSubmitOrderOptions>(Configuration.GetSection("JOBS:RetryUrgentSubmitOrder"));
            services.Configure<JobsOrderReminderOptions>(Configuration.GetSection("JOBS:OrderReminder"));
            services.Configure<CleanupSignalRMessagesJobOptions>(Configuration.GetSection("JOBS:CleanupSignalRMessages"));

            services.AddLogging(options =>
            {
                options.Services.Configure<SimpleConsoleFormatterOptions>(c =>
                {
                    c.ColorBehavior = LoggerColorBehavior.Enabled;
                    c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
                });

                options.AddSplunk(c =>
                {
                    c.SplunkCollectorUrl = Configuration.GetNonEmptyValue("SplunkCollectorUrl");
                    c.AuthenticationToken = Configuration.GetNonEmptyValue("SplunkToken");
                });
            });

            services.AddSingleton<MigrationAndSeedService>();

            services.AddDbContext<ScvDbContext>(options =>
                {
                    var connectionString = Configuration.GetNonEmptyValue("DatabaseConnectionString");

                    options.UseNpgsql(connectionString, npg =>
                    {
                        npg.MigrationsAssembly("db");
                        npg.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
                    }).UseSnakeCaseNamingConvention();

                    if (CurrentEnvironment.IsDevelopment())
                        options.EnableSensitiveDataLogging();
                }
            );

            services.AddScoped(typeof(IPostgresRepositoryBase<,>), typeof(PostgresRepositoryBase<,>));
            services.AddScoped<INotificationRepository, NotificationRepository>();

            services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationRedirectMiddlewareResultHandler>();

            services.AddMapster();
            services.AddNutrient(Configuration);
            services.AddJasperDb(Configuration);

            services.AddAWSConfig(Configuration);
            services.AddHangfire(Configuration);
            services.AddGraphService(Configuration);
            services.AddClamAv(Configuration);

            services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize =
                    Configuration.GetValue<long>("SignalR:MaximumReceiveMessageSizeBytes");
                options.KeepAliveInterval = TimeSpan.FromSeconds(
                    Configuration.GetValue<int>("SignalR:KeepAliveIntervalSeconds"));
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(
                    Configuration.GetValue<int>("SignalR:ClientTimeoutIntervalSeconds"));
            });
            services.AddSingleton<IUserIdProvider, UserIdProvider>();
            services.AddSignalRPostgresBackplane(Configuration);

            #region Cors

            var origins = ParseOrigins(
                Configuration.GetValue<string>("CORS_DOMAIN"),
                Configuration.GetValue<string>("PublicCorsDomain")
            );

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            #endregion Cors

            #region Setup Services

            services.AddHttpClientsAndScvServices(Configuration);

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            services.Configure<TdApiOptions>(Configuration.GetSection("TDApi"));

            #endregion Setup Services

            services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

            #region Data Protection
            services.AddSingleton(new AesGcmEncryptionOptions { Key = Configuration.GetNonEmptyValue("DataProtectionKeyEncryptionKey") });

            services.AddDataProtection()
                .PersistKeysToDbContext<ScvDbContext>()
                .UseXmlEncryptor(s => new AesGcmXmlEncryptor(s))
                .SetApplicationName("SCV");

            #endregion Data Protection

            #region Authentication & Authorization
            services.AddScvAuthentication(CurrentEnvironment, Configuration);

            services.AddScvAuthorization();
            #endregion

            #region Newtonsoft

            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
                options.SerializerSettings.Formatting = Formatting.Indented;
                options.SerializerSettings.Converters.Add(new StringEnumConverter());
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });

            #endregion Newtonsoft

            #region Swagger

            services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations(true, true);
                options.CustomSchemaIds(o => o.FullName);
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });

            services.AddSwaggerGenNewtonsoftSupport();

            #endregion Swagger

            services.AddLazyCache(serviceProvider =>
            {
                var cache = new LazyCache.CachingService
                {
                    DefaultCachePolicy = new LazyCache.CacheDefaults
                    {
                        DefaultCacheDurationSeconds = Configuration.GetValue<int?>("LAZY_CACHE_DEFAULT_DURATION_SECONDS") ?? 300 // 5 minutes
                    }
                };
                return cache;
            });

            services.AddHttpContextAccessor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Configure DateTimeExtensions to use the service provider
            DateTimeExtensions.Configure(app.ApplicationServices);

            // Use the new exception handler
            app.UseExceptionHandler();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Use((context, next) =>
            {
                context.Request.EnableBuffering();
                context.Request.Scheme = "https";
                if (context.Request.Headers.ContainsKey("X-Forwarded-Host") && !env.IsDevelopment())
                {
                    var baseUrl = context.Request.Headers["X-Base-Href"].ToString();
                    context.Request.PathBase = new PathString(baseUrl[..^1]);
                }
                return next();
            });

            app.UseForwardedHeaders();
            app.UseCors();

            app.UseSwagger(options =>
            {
                options.RouteTemplate = "api/swagger/{documentname}/swagger.json";
                options.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    if (!httpReq.Headers.TryGetValue("X-Forwarded-Host", out Microsoft.Extensions.Primitives.StringValues forwardedHost))
                        return;
                    var forwardedPort = httpReq.Headers["X-Forwarded-Port"];
                    var baseUrl = httpReq.Headers["X-Base-Href"];
                    swaggerDoc.Servers =
                    [
                        new OpenApiServer { Url = XForwardedForHelper.BuildUrlString(forwardedHost, forwardedPort, baseUrl) }
                    ];
                });
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("swagger/v1/swagger.json", "SCV.API");
                options.RoutePrefix = "api";
            });

            app.UseMiddleware<ErrorHandlingMiddleware>();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            var connectionString = Configuration.GetValue<string>("DatabaseConnectionString");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    Authorization = [new HangFireDashboardAuthorizationFilter()]
                });
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<NotificationsHub>("/api/notifications");
                endpoints.MapHealthChecks("/api/health", new HealthCheckOptions
                {
                    ResponseWriter = HealthCheckResponseWriter.WriteResponse,
                });
            });
        }
#nullable enable
        private static string[] ParseOrigins(params string?[] rawValues)
        {
            return [.. rawValues
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .SelectMany(v => v!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(o => o.Trim('"', '\''))
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Distinct()];
        }
    }
}

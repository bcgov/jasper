using ColeSoft.Extensions.Logging.Splunk;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Console;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Infrastructure;
using Scv.Api.Infrastructure.Authentication;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Infrastructure.Encryption;
using Scv.Api.Infrastructure.Middleware;
using Scv.Api.Jobs;
using Scv.Api.Services.EF;
using Scv.Db.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Scv.Api
{
    public class Startup
    {
        private IWebHostEnvironment CurrentEnvironment { get; }

        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
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
                    options.UseNpgsql(Configuration.GetNonEmptyValue("DatabaseConnectionString"), npg =>
                    {
                        npg.MigrationsAssembly("db");
                        npg.EnableRetryOnFailure(5, TimeSpan.FromSeconds(1), null);
                    }).UseSnakeCaseNamingConvention();

                    if (CurrentEnvironment.IsDevelopment())
                        options.EnableSensitiveDataLogging();
                }
            );

            services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationRedirectMiddlewareResultHandler>();

            services.AddMapster();
            services.AddNutrient();
            services.AddJasperDb(Configuration);
            //services.AddHangfire(Configuration);

            #region Cors

            string corsDomain = Configuration.GetValue<string>("CORS_DOMAIN");

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins(corsDomain);
                });
            });

            #endregion Cors

            #region Setup Services

            services.AddHttpClientsAndScvServices(Configuration);

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

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

            services.AddLazyCache();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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
                    context.Request.PathBase = new PathString(baseUrl.Remove(baseUrl.Length - 1));
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
                    if (!httpReq.Headers.ContainsKey("X-Forwarded-Host"))
                        return;

                    var forwardedHost = httpReq.Headers["X-Forwarded-Host"];
                    var forwardedPort = httpReq.Headers["X-Forwarded-Port"];
                    var baseUrl = httpReq.Headers["X-Base-Href"];
                    swaggerDoc.Servers = new List<OpenApiServer>
                    {
                        new OpenApiServer { Url = XForwardedForHelper.BuildUrlString(forwardedHost, forwardedPort, baseUrl) }
                    };
                });
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("swagger/v1/swagger.json", "SCV.API");
                options.RoutePrefix = "api";
            });

            app.UseMiddleware(typeof(ErrorHandlingMiddleware));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // var connectionString = Configuration.GetValue<string>("MONGODB_CONNECTION_STRING");
            // var dbName = Configuration.GetValue<string>("MONGODB_NAME");
            // if (!string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(dbName))
            // {
            //     app.UseHangfireDashboard("/hangfire", new DashboardOptions
            //     {
            //         Authorization = [new HangFireDashboardAuthorizationFilter()]
            //     });

            //     #region Setup Jobs
            //     using (var scope = app.ApplicationServices.CreateScope())
            //     {
            //         var provider = scope.ServiceProvider;
            //         var allJobs = provider.GetServices<IRecurringJob>();

            //         foreach (var job in allJobs)
            //         {
            //             RecurringJobHelper.AddOrUpdate(job);
            //         }
            //     }
            //     #endregion Setup Jobs
            // }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
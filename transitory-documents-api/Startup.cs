using ColeSoft.Extensions.Logging.Splunk;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Polly;
using Polly.Retry;
using Scv.Core.Helpers;
using Scv.Core.Helpers.ContractResolver;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure.Authorization;
using Scv.Core.Infrastructure.Handler;
using Scv.TdApi.Infrastructure.Authorization;
using Scv.TdApi.Infrastructure.FileSystem;
using Scv.TdApi.Infrastructure.Middleware;
using Scv.TdApi.Infrastructure.Options;
using Scv.TdApi.Services;
using System.Reflection;
using System.Security.Claims;

namespace Scv.TdApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
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

            #region Cors

            string corsDomain = Configuration.GetValue<string>("CORS_DOMAIN") ?? "*";

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins(corsDomain)
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });

            services.AddHealthChecks();

            #endregion Cors

            #region Options Configuration

            services.AddOptions<KeycloakOptions>()
                .Bind(Configuration.GetSection("Keycloak"))
                .ValidateDataAnnotations()
                .Validate(o => !string.IsNullOrWhiteSpace(o.Authority), "Keycloak:Authority is required")
                .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Keycloak:Audience is required")
                .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId), "Keycloak:ClientId is required")
                .ValidateOnStart();

            services.AddOptions<SharedDriveOptions>()
                .Bind(Configuration.GetSection("SharedDrive"))
                .ValidateDataAnnotations()
                .Validate(o => !string.IsNullOrWhiteSpace(o.BasePath), "SharedDrive:BasePath is required")
                .Validate(o => o.DateFolderFormats is { Count: > 0 }, "SharedDrive:DateFolderFormats must have at least one format")
                .ValidateOnStart();

            services.AddOptions<CorrectionMappingOptions>()
                .Bind(Configuration.GetSection("CorrectionMappings"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            #endregion Options Configuration

            #region Setup Services

            services.AddHttpContextAccessor();
            services.AddTransient(s => s.GetService<IHttpContextAccessor>()?.HttpContext?.User ?? new ClaimsPrincipal());
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            services.AddSingleton<ISmbClientFactory, SmbClientFactory>();
            services.AddScoped<ISmbFileSystemClient, SmbFileSystemClient>();

            services.AddScoped<ISharedDriveFileService, SharedDriveFileService>();

            #endregion Setup Services

            services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

            #region Authentication & Authorization

            // Add JWT Bearer Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var keycloakOptions = Configuration.GetSection("Keycloak").Get<KeycloakOptions>()
                    ?? new KeycloakOptions();

                options.Authority = keycloakOptions.Authority;
                options.Audience = keycloakOptions.Audience;
                options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
                options.SaveToken = false; // No need to save tokens for API-to-API calls

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = keycloakOptions.ValidateIssuer,
                    ValidIssuer = keycloakOptions.Authority,
                    ValidateAudience = true,
                    ValidAudience = keycloakOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30), // Allow 30 seconds clock skew
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Startup>>();

                        logger.LogWarning(
                            context.Exception,
                            "JWT authentication failed: {Message}",
                            context.Exception.Message);

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Startup>>();

                        var principal = context.Principal;
                        var username = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? principal?.FindFirst("preferred_username")?.Value
                            ?? "unknown";

                        var clientId = principal?.FindFirst("azp")?.Value
                            ?? principal?.FindFirst("client_id")?.Value;

                        logger.LogInformation(
                            "JWT token validated for user: {Username}, client: {ClientId}",
                            username,
                            clientId);

                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Startup>>();

                        logger.LogWarning(
                            "Authentication challenge: {Error}, {ErrorDescription}",
                            context.Error,
                            context.ErrorDescription);

                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Startup>>();

                        var principal = context.Principal;
                        var username = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? principal?.FindFirst("preferred_username")?.Value
                            ?? "unknown";

                        logger.LogWarning(
                            "Access forbidden for user: {Username}",
                            username);

                        return Task.CompletedTask;
                    }
                };
            });

            services.AddScoped<IAuthorizationHandler, PermissionHandler>();
            services.AddScoped<IAuthorizationHandler, TdRoleAuthorizationHandler>();

            // Configure authorization policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy(TdPolicies.RequireQueryRole, policy =>
                {
                    policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new TdRoleRequirement(TdRoles.Query));
                });

                options.AddPolicy(TdPolicies.RequireReadRole, policy =>
                {
                    policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new TdRoleRequirement(TdRoles.Read));
                });
            });

            services.AddTransient<TimingHandler>();

            #endregion Authentication & Authorization

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
                var thisAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                options.EnableAnnotations(true, true);
                options.CustomSchemaIds(o => o.FullName);

                var xmlFile = $"{thisAssemblyName}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);

                options.DocInclusionPredicate((docName, apiDesc) =>
                {
                    if (apiDesc.ActionDescriptor is ControllerActionDescriptor cad)
                    {
                        var controllerAsm = cad.ControllerTypeInfo.Assembly.GetName().Name;
                        return string.Equals(controllerAsm, thisAssemblyName, StringComparison.OrdinalIgnoreCase);
                    }

                    return false;
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            services.AddSwaggerGenNewtonsoftSupport();

            #endregion Swagger

            services.AddLazyCache();

            services.AddSingleton<AsyncRetryPolicy>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<SharedDriveOptions>>().Value;

                return Policy
                    .Handle<IOException>(ex => ex is not FileNotFoundException && ex is not DirectoryNotFoundException)
                    .Or<Exception>(ex =>
                        ex is not FileNotFoundException &&
                        ex is not DirectoryNotFoundException &&
                        ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
                    .WaitAndRetryAsync(
                        retryCount: options.MaxRetryAttempts,
                        sleepDurationProvider: retryAttempt =>
                            TimeSpan.FromMilliseconds(options.InitialRetryDelayMs * Math.Pow(2, retryAttempt - 1)),
                        onRetry: (exception, timeSpan, retryCount, context) =>
                        {
                            var logger = serviceProvider.GetRequiredService<ILogger<SmbFileSystemClient>>();
                            logger.LogWarning(
                                exception,
                                "SMB operation failed. Retry {RetryCount}/{MaxRetries} after {Delay}ms",
                                retryCount,
                                options.MaxRetryAttempts,
                                timeSpan.TotalMilliseconds);
                        });
            });
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
                    if (!string.IsNullOrEmpty(baseUrl) && baseUrl.Length > 0)
                    {
                        context.Request.PathBase = new PathString(baseUrl.Remove(baseUrl.Length - 1));
                    }
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

                    var forwardedHost = httpReq.Headers["X-Forwarded-Host"].ToString();
                    var forwardedPort = httpReq.Headers["X-Forwarded-Port"].ToString();
                    var baseUrl = httpReq.Headers["X-Base-Href"].ToString();

                    swaggerDoc.Servers = new List<OpenApiServer>
                    {
                        new OpenApiServer { Url = XForwardedForHelper.BuildUrlString(forwardedHost, forwardedPort, baseUrl) }
                    };
                });
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("swagger/v1/swagger.json", "Transitory Documents API");
                options.RoutePrefix = "api";
            });

            app.UseMiddleware(typeof(TdErrorHandlingMiddleware));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}
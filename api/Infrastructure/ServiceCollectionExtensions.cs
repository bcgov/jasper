using System;
using System.Net.Http;
using System.Reflection;
using JCCommon.Clients.FileServices;
using JCCommon.Clients.LocationServices;
using JCCommon.Clients.LookupCodeServices;
using JCCommon.Clients.UserService;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Infrastructure.Encryption;
using Scv.Api.Infrastructure.Handler;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Services;
using Scv.Api.Services.Files;
using Scv.Db.Contexts;
using Scv.Db.Repositories;
using Scv.Db.Seeders;
using BasicAuthenticationHeaderValue = JCCommon.Framework.BasicAuthenticationHeaderValue;
using PCSSCourtCalendarServices = PCSSCommon.Clients.CourtCalendarServices;
using PCSSFileDetailServices = PCSSCommon.Clients.FileDetailServices;
using PCSSJudicialCalendarServices = PCSSCommon.Clients.JudicialCalendarServices;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;
using PCSSLookupServices = PCSSCommon.Clients.LookupServices;
using PCSSReportServices = PCSSCommon.Clients.ReportServices;
using PCSSSearchDateServices = PCSSCommon.Clients.SearchDateServices;

namespace Scv.Api.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        const string X_APIGW_KEY_HEADER = "x-api-key";
        const string X_ORIGIN_VERIFY_HEADER = "x-origin-verify";
        const string X_TARGET_APP = "x-target-app";

        public static IServiceCollection AddMapster(this IServiceCollection services, Action<TypeAdapterConfig> options = null)
        {
            var config = TypeAdapterConfig.GlobalSettings;
            config.Scan(Assembly.GetAssembly(typeof(Startup)) ?? throw new InvalidOperationException());

            options?.Invoke(config);

            services.AddSingleton(config);
            services.AddScoped<IMapper>(sp => new Mapper(sp.GetRequiredService<TypeAdapterConfig>()));

            return services;
        }

        public static IServiceCollection AddAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(Startup));

            return services;
        }

        public static IServiceCollection AddJasperDb(this IServiceCollection services, IConfiguration configuration)
        {
            // Remove checking when the "real" mongo db has been configured
            var connectionString = configuration.GetValue<string>("MONGODB_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
            {
                return services;
            }

            services.AddScoped<PermissionSeeder>();
            services.AddScoped<RoleSeeder>();
            services.AddScoped<GroupSeeder>();
            services.AddScoped<UserSeeder>();

            services.AddDbContext<JasperDbContext>(options =>
            {
                var dbName = configuration.GetValue<string>("MONGODB_NAME");
                options.UseMongoDB(connectionString, dbName);
            });

            services.AddTransient<SeederFactory<JasperDbContext>>();

            services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));
            services.AddScoped<IPermissionRepository, PermissionRepository>();

            return services;
        }

        public static IServiceCollection AddHttpClientsAndScvServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<TimingHandler>();

            // JC Interface
            services
                .AddHttpClient<FileServicesClient>(client => { ConfigureHttpClient(client, configuration, "FileServicesClient", 300); })
                .AddHttpMessageHandler<TimingHandler>();
            services
                .AddHttpClient<LookupCodeServicesClient>(client => { ConfigureHttpClient(client, configuration, "LookupServicesClient"); })
                .AddHttpMessageHandler<TimingHandler>();
            services
                .AddHttpClient<LocationServicesClient>(client => { ConfigureHttpClient(client, configuration, "LocationServicesClient"); })
                .AddHttpMessageHandler<TimingHandler>();
            services
                .AddHttpClient<UserServiceClient>(client => { ConfigureHttpClient(client, configuration, "UserServicesClient"); })
                .AddHttpMessageHandler<TimingHandler>();

            // PCSS
            services
                .AddHttpClient<PCSSLocationServices.LocationServicesClient>(
                    typeof(PCSSLocationServices.LocationServicesClient).FullName,
                    (client) => { ConfigureHttpClient(client, configuration, "PCSS"); })
                .AddHttpMessageHandler<TimingHandler>();
            services
                .AddHttpClient<PCSSCourtCalendarServices.CourtCalendarClientServicesClient>(client => { ConfigureHttpClient(client, configuration, "PCSS"); })
                .AddHttpMessageHandler<TimingHandler>();
            services
                .AddHttpClient<PCSSJudicialCalendarServices.JudicialCalendarServicesClient>(client => { ConfigureHttpClient(client, configuration, "PCSS"); })
                .AddHttpMessageHandler<TimingHandler>();
            services
                .AddHttpClient<PCSSSearchDateServices.SearchDateClient>(client => { ConfigureHttpClient(client, configuration, "PCSS"); })
                .AddHttpMessageHandler<TimingHandler>();
            services
                .AddHttpClient<PCSSFileDetailServices.FileDetailClient>(client => { ConfigureHttpClient(client, configuration, "PCSS"); })
                .AddHttpMessageHandler<TimingHandler>();
            services
                .AddHttpClient<PCSSLookupServices.LookupServicesClient>(client => { ConfigureHttpClient(client, configuration, "PCSS"); })
                .AddHttpMessageHandler<TimingHandler>();
            services
                .AddHttpClient<PCSSReportServices.ReportServicesClient>(client => { ConfigureHttpClient(client, configuration, "PCSS"); })
                .AddHttpMessageHandler<TimingHandler>();

            services.AddHttpContextAccessor();
            services.AddTransient(s => s.GetService<IHttpContextAccessor>().HttpContext.User);
            services.AddScoped<FilesService>();
            services.AddScoped<LookupService>();
            services.AddScoped<LocationService>();
            services.AddScoped<CourtListService>();
            services.AddScoped<VcCivilFileAccessHandler>();
            services.AddSingleton<JCUserService>();
            services.AddSingleton<AesGcmEncryption>();
            services.AddSingleton<JudicialCalendarService>();


            var connectionString = configuration.GetValue<string>("MONGODB_CONNECTION_STRING");
            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddScoped<IAccessControlManagementService<PermissionDto>, PermissionService>();
                services.AddScoped<IAccessControlManagementService<RoleDto>, RoleService>();
                services.AddScoped<IAccessControlManagementService<GroupDto>, GroupService>();
                services.AddScoped<IUserService, UserService>();
            }

            return services;
        }

        private static void ConfigureHttpClient(HttpClient client, IConfiguration configuration, string prefix, int timeoutInSecs = 100)
        {
            var apigwUrl = configuration.GetValue<string>("AWS_API_GATEWAY_URL");
            var apigwKey = configuration.GetValue<string>("AWS_API_GATEWAY_API_KEY");
            var authorizerKey = configuration.GetValue<string>("AuthorizerKey");

            client.Timeout = TimeSpan.FromSeconds(timeoutInSecs);

            // Defaults to BC Gov API if any config setting is missing
            if (string.IsNullOrWhiteSpace(apigwUrl) || string.IsNullOrWhiteSpace(apigwKey) || string.IsNullOrWhiteSpace(authorizerKey))
            {
                Console.WriteLine($"Redirecting traffic to: {configuration.GetNonEmptyValue($"{prefix}:Url")}");
                client.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(
                    configuration.GetNonEmptyValue($"{prefix}:Username"),
                    configuration.GetNonEmptyValue($"{prefix}:Password"));
                client.BaseAddress = new Uri(configuration.GetNonEmptyValue($"{prefix}:Url").EnsureEndingForwardSlash());
            }
            // Requests are routed to JASPER's API Gateway. Lambda functions are triggered by these requests and are responsible for communicating with the BC Gov API.
            else
            {
                Console.WriteLine($"Redirecting traffic to: {apigwUrl}");
                client.BaseAddress = new Uri(apigwUrl.EnsureEndingForwardSlash());
                client.DefaultRequestHeaders.Add(X_APIGW_KEY_HEADER, apigwKey);
                client.DefaultRequestHeaders.Add(X_ORIGIN_VERIFY_HEADER, authorizerKey);
                // The prefix will help determine where will the request is routed (e.g. lookup, CatsAPI or DARS)
                client.DefaultRequestHeaders.Add(X_TARGET_APP, prefix);
            }
        }
    }
}

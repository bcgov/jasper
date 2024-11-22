using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Mapster;
using MapsterMapper;
using System.Reflection;
using JCCommon.Clients.FileServices;
using JCCommon.Clients.LocationServices;
using JCCommon.Clients.LookupCodeServices;
using JCCommon.Clients.UserService;
using Scv.Api.Infrastructure.Encryption;
using Scv.Api.Services;
using Scv.Api.Services.Files;
using BasicAuthenticationHeaderValue = JCCommon.Framework.BasicAuthenticationHeaderValue;
using Scv.Api.Infrastructure.Handler;
using PCSSClient.Clients.JudicialCalendarsServices;
using PCSSClient.Clients.CourtCalendarsServices;


namespace Scv.Api.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        const string X_APIGW_KEY_HEADER = "x-api-key";
        const string X_ORIGIN_VERIFY_HEADER = "x-origin-verify";

        public static IServiceCollection AddMapster(this IServiceCollection services, Action<TypeAdapterConfig> options = null)
        {
            var config = TypeAdapterConfig.GlobalSettings;
            config.Scan(Assembly.GetAssembly(typeof(Startup)) ?? throw new InvalidOperationException());

            options?.Invoke(config);

            services.AddSingleton(config);
            services.AddScoped<IMapper>(sp => new Mapper(sp.GetRequiredService<TypeAdapterConfig>()));

            return services;
        }

        public static IServiceCollection AddHttpClientsAndScvServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<TimingHandler>();
            services.AddHttpClient<FileServicesClient>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                client.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(
                    configuration.GetNonEmptyValue("FileServicesClient:Username"),
                    configuration.GetNonEmptyValue("FileServicesClient:Password"));
                client.BaseAddress = new Uri(configuration.GetNonEmptyValue("FileServicesClient:Url").EnsureEndingForwardSlash());
            }).AddHttpMessageHandler<TimingHandler>();

            services.AddHttpClient<LookupCodeServicesClient>(client =>
            {
                client.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(
                    configuration.GetNonEmptyValue("LookupServicesClient:Username"),
                    configuration.GetNonEmptyValue("LookupServicesClient:Password"));
                client.BaseAddress = new Uri(configuration.GetNonEmptyValue("LookupServicesClient:Url").EnsureEndingForwardSlash());
            }).AddHttpMessageHandler<TimingHandler>();

            services.AddHttpClient<LocationServicesClient>(client =>
            {
                client.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(
                    configuration.GetNonEmptyValue("LocationServicesClient:Username"),
                    configuration.GetNonEmptyValue("LocationServicesClient:Password"));
                client.BaseAddress = new Uri(configuration.GetNonEmptyValue("LocationServicesClient:Url").EnsureEndingForwardSlash());
            }).AddHttpMessageHandler<TimingHandler>();

            services.AddHttpClient<UserServiceClient>(client =>
            {
                client.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(
                    configuration.GetNonEmptyValue("UserServicesClient:Username"),
                    configuration.GetNonEmptyValue("UserServicesClient:Password"));
                client.BaseAddress = new Uri(configuration.GetNonEmptyValue("UserServicesClient:Url")
                    .EnsureEndingForwardSlash());
            }).AddHttpMessageHandler<TimingHandler>();

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

            services.AddSingleton<JudicialCalendarsServicesClient>();
            services.AddSingleton<CourtCalendarsServicesClient>();

            return services;
        }
    }
}

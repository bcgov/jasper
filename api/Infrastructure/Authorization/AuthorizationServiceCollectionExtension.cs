using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Scv.Core.Infrastructure.Authorization;
using static Scv.Api.Infrastructure.Authorization.ProviderAuthorizationHandler;

namespace Scv.Api.Infrastructure.Authorization
{
    public static class AuthorizationServiceCollectionExtension
    {
        public static IServiceCollection AddScvAuthorization(this IServiceCollection services)
        {
            services.AddScoped<IAuthorizationHandler, ProviderAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, PermissionHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(nameof(ProviderAuthorizationHandler), policy =>
                    policy.Requirements.Add(new ProviderRequirement()));
            });

            return services;
        }
    }
}

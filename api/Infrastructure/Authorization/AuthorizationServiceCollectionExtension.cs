using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Scv.Api.Infrastructure.Authorization
{
    public static class AuthorizationServiceCollectionExtension
    {
        public static IServiceCollection AddScvAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(nameof(ProviderAuthorizationHandler), policy => policy.Requirements.Add(new ProviderAuthorizationHandler()));
            });

            services.AddScoped<IAuthorizationHandler, PermissionHandler>();

            return services;
        }
    }
}

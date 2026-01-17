using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using static Scv.Api.Infrastructure.Authorization.ProviderAuthorizationHandler;

namespace Scv.Api.Infrastructure.Authorization
{
    public static class AuthorizationServiceCollectionExtension
    {
        public static IServiceCollection AddScvAuthorization(this IServiceCollection services)
        {
            services.AddScoped<IAuthorizationHandler, ProviderAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, CSoRoleAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, PermissionHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(nameof(ProviderAuthorizationHandler), policy =>
                    policy.Requirements.Add(new ProviderRequirement()));
                options.AddPolicy(CsoPolicies.RequireWriteRole, policy =>
                    policy.Requirements.Add(new CsoRoleRequirement(CsoRoles.Write)));
            });

            return services;
        }
    }
}

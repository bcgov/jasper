using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Scv.Api.Controllers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authentication;
using Scv.Api.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Scv.Api.Infrastructure.Authorization.ProviderAuthorizationHandler;

namespace Scv.Api.Infrastructure.Authorization
{
    public class ProviderAuthorizationHandler : AuthorizationHandler<ProviderRequirement>, IAuthorizationRequirement
    {
        private readonly IUserService _userService;

        public sealed class ProviderRequirement : IAuthorizationRequirement
        {
            public ProviderRequirement() { }
        }

        public ProviderAuthorizationHandler(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// SiteMinder - specific Participant Id and AgencyId from CCD <see cref="SiteMinderAuthenticationHandler"/>
        /// IDIR - generic Participant Id and AgencyId. - Uses <see cref="OpenIdConnectHandler"/>
        /// VC - specific Participant Id and AgencyId from A2A, with a fall back to a generic - limited routes - Uses <see cref="OpenIdConnectHandler"/>
        /// </summary>
        protected async override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProviderRequirement requirement)
        {
            var user = context.User;
            var httpContext = context.Resource as DefaultHttpContext;
            var endpoint = httpContext?.GetEndpoint();
            var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
            var isAuthController = actionDescriptor?.ControllerTypeInfo?.Name == nameof(AuthController);
            var isUserController = actionDescriptor?.ControllerTypeInfo?.Name == nameof(UsersController);

            var userId = context.User.UserId();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var databaseUser = await _userService.GetByIdWithPermissionsAsync(userId);
                if (databaseUser == null || context.User.HasChanged(databaseUser))
                {
                    context.Fail(new AuthorizationFailureReason(this, "User claims changed since last authentication."));
                    return;
                }
            }

            if (isUserController && (actionDescriptor.ActionName == nameof(UsersController.RequestAccess) || actionDescriptor.ActionName == nameof(UsersController.GetMyUser)))
            {
                context.Succeed(requirement);
                return;
            }

            if (user.Identity.AuthenticationType == SiteMinderAuthenticationHandler.SiteMinder)
            {
                context.Succeed(requirement);
                return;
            }

            if (user.Groups().Contains("court-viewer-supreme") || user.Groups().Contains("court-viewer-provincial"))
            {
                context.Succeed(requirement);
                return;
            }

            if (user.IsVcUser() && endpoint != null)
            {

                var isFilesController = actionDescriptor?.ControllerTypeInfo.Name == nameof(FilesController);


                var allowedActionsForVc = new List<string>
                {
                    nameof(FilesController.GetCivilFileDetailByFileId),
                    nameof(FilesController.GetCivilCourtSummaryReport),
                    nameof(FilesController.GetDocument),
                    nameof(FilesController.GetArchive)
                };

                if (isFilesController && allowedActionsForVc.Contains(actionDescriptor.ActionName))
                {
                    context.Succeed(requirement);
                    return;
                }
                if (isAuthController && actionDescriptor.ActionName == nameof(AuthController.UserInfo))
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}

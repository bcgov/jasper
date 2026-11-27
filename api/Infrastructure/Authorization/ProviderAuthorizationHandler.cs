using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.IdentityModel.Tokens;
using Scv.Api.Controllers;
using Scv.Api.Infrastructure.Authentication;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using Scv.Models.AccessControlManagement;
using static Scv.Api.Infrastructure.Authorization.ProviderAuthorizationHandler;

namespace Scv.Api.Infrastructure.Authorization
{
    public class ProviderAuthorizationHandler(IUserService userService) : AuthorizationHandler<ProviderRequirement>, IAuthorizationRequirement
    {
        private readonly IUserService _userService = userService;

        public sealed class ProviderRequirement : IAuthorizationRequirement
        {
            public ProviderRequirement() { }
        }

        /// <summary>
        /// SiteMinder - specific Participant Id and AgencyId from CCD <see cref="SiteMinderAuthenticationHandler"/>
        /// IDIR - generic Participant Id and AgencyId. - Uses <see cref="OpenIdConnectHandler"/>
        /// VC - specific Participant Id and AgencyId from A2A, with a fall back to a generic - limited routes - Uses <see cref="OpenIdConnectHandler"/>
        /// </summary>
        protected async override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProviderRequirement requirement)
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                return;
            }

            var httpContext = context.Resource as DefaultHttpContext;
            var endpoint = httpContext?.GetEndpoint();
            var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
            var isUserController = actionDescriptor?.ControllerTypeInfo?.Name == nameof(UsersController);
            var isRequestAccess = isUserController && actionDescriptor.ActionName == nameof(UsersController.RequestAccess);
            var isAuthInfo = actionDescriptor?.ControllerTypeInfo?.Name == nameof(AuthController)
                && actionDescriptor.ActionName == nameof(AuthController.UserInfo);
            var isPcssUser = user.ProvjudUserGuid() != null;

            var userId = context.User.UserId();
            UserDto databaseUser = null;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                databaseUser = await _userService.GetByIdWithPermissionsAsync(userId);
                if (databaseUser == null || (!isRequestAccess && context.User.HasChanged(databaseUser)))
                {
                    context.Fail(new AuthorizationFailureReason(this, "User claims changed since last authentication."));
                    return;
                }
            }

            if (isRequestAccess || (isUserController && actionDescriptor.ActionName == nameof(UsersController.GetMyUser))
                || (isAuthInfo && isPcssUser && databaseUser != null))
            {
                context.Succeed(requirement);
                return;
            }

            if (isPcssUser)
            {
                if (databaseUser?.IsActive == true && databaseUser.GroupIds?.Count > 0 && databaseUser.Roles?.Count > 0)
                {
                    context.Succeed(requirement);
                }
                return;
            }

            // Authorize the remaining providers using their existing group rules.
            if (user.Identity.AuthenticationType == SiteMinderAuthenticationHandler.SiteMinder
                || (user.Identity.AuthenticationType == TokenValidationParameters.DefaultAuthenticationType && user.Groups().Count > 0))
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
            }
        }
    }
}

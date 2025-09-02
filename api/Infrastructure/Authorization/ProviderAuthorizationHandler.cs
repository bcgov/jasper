﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Scv.Api.Controllers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authentication;

namespace Scv.Api.Infrastructure.Authorization
{
    public class ProviderAuthorizationHandler : AuthorizationHandler<ProviderAuthorizationHandler>, IAuthorizationRequirement
    {
        /// <summary>
        /// SiteMinder - specific Participant Id and AgencyId from CCD <see cref="SiteMinderAuthenticationHandler"/>
        /// IDIR - generic Participant Id and AgencyId. - Uses <see cref="OpenIdConnectHandler"/>
        /// VC - specific Participant Id and AgencyId from A2A, with a fall back to a generic - limited routes - Uses <see cref="OpenIdConnectHandler"/>
        /// </summary>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ProviderAuthorizationHandler requirement)
        {
            var user = context.User;
            var httpContext = context.Resource as DefaultHttpContext;
            var endpoint = httpContext?.GetEndpoint();
            var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
            var isAuthController = actionDescriptor?.ControllerTypeInfo?.Name == nameof(AuthController);
            var isUserController = actionDescriptor?.ControllerTypeInfo?.Name == nameof(UsersController);

            if (isUserController && (actionDescriptor.ActionName == nameof(UsersController.RequestAccess) || actionDescriptor.ActionName == nameof(UsersController.GetMyUser)))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (!user.IsActive())
            {
                context.Fail();
            }

            if (user.Identity.AuthenticationType == SiteMinderAuthenticationHandler.SiteMinder)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (user.Groups().Contains("court-viewer-supreme") || user.Groups().Contains("court-viewer-provincial"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (user.IsVcUser() && endpoint != null)
            {

                var isFilesController = actionDescriptor?.ControllerTypeInfo.Name == nameof(FilesController);


                var allowedActionsForVc = new List<string>
                {
                    nameof(FilesController.GetCivilFileDetailByFileId),
                    nameof(FilesController.GetCivilCourtSummaryReport),
                    nameof(FilesController.GetDocument),
                    nameof(FilesController.GetArchive),
                    nameof(FilesController.GetCivilAppearanceDetails)
                };

                if (isFilesController && allowedActionsForVc.Contains(actionDescriptor.ActionName))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
                if (isAuthController && actionDescriptor.ActionName == nameof(AuthController.UserInfo))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }

            context.Fail();
            return Task.CompletedTask;
        }
    }
}

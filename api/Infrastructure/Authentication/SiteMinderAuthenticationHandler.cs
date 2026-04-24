using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Services;
using Scv.Core.Helpers;
using Scv.Core.Helpers.Extensions;
using Scv.Models.JCUserService;

namespace Scv.Api.Infrastructure.Authentication
{
    public class SiteMinderAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration, JCUserService jcUserService) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        #region Properties 
        public const string SiteMinder = nameof(SiteMinder);
        private JCUserService JCUserService { get; } = jcUserService;
        private string ValidSiteMinderUserType { get; } = configuration.GetNonEmptyValue("Auth:AllowSiteMinderUserType");

        #endregion
        #region Constructor
        #endregion

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Logger.LogInformation("Authenticating with SiteMinder");
            string siteMinderUserGuidHeader = Request.Headers["SMGOV_USERGUID"];
            string siteMinderUserTypeHeader = Request.Headers["SMGOV_USERTYPE"];
            Logger.LogDebug("SMGOV_USERGUID: {SiteMinderUserGuidHeader}", siteMinderUserGuidHeader);
            Logger.LogDebug("SMGOV_USERTYPE: {SiteMinderUserTypeHeader}", siteMinderUserTypeHeader);

            if (siteMinderUserGuidHeader == null || siteMinderUserTypeHeader == null)
            {
                Logger.LogDebug("One of the SiteMinder headers was null: siteMinderUserGuidHeader: {SiteMinderUserGuidHeader}, siteMinderUserTypeHeader: {SiteMinderUserTypeHeader}", siteMinderUserGuidHeader, siteMinderUserTypeHeader);
                return AuthenticateResult.NoResult();
            }
            else
            {
                Logger.LogDebug("Siteminder headers accounted for");
            }

            if (siteMinderUserTypeHeader != ValidSiteMinderUserType)
            {
                Logger.LogDebug("USERTYPE does not match ValidSiteMinderUserType: {SiteMinderUserTypeHeader} vs {ValidSiteMinderUserType}", siteMinderUserTypeHeader, ValidSiteMinderUserType);
                return AuthenticateResult.Fail("Invalid SiteMinder UserType Header.");
            }
            else
            {
                Logger.LogDebug("SiteMinder user type is valid");
            }

            // set authtype when we get guid, find out where this context is being set for identity
            Logger.LogDebug("Context.User.Identity all:");
            Logger.LogDebug("\tContext.User.Identity.AuthenticationType: {ContextUserIdentityAuthenticationType}", Context.User.Identity.AuthenticationType);
            Logger.LogDebug("\tContext.User.Identity.IsAuthenticated: {ContextUserIdentityIsAuthenticated}", Context.User.Identity.IsAuthenticated);
            Logger.LogDebug("\tContext.User.Identity.Name: {ContextUserIdentityName}", Context.User.Identity.Name);

            var authenticatedBySiteMinderPreviously = Context.User.Identity.AuthenticationType == SiteMinder;
            var participantId = Context.User.ParticipantId();
            var agencyCode = Context.User.AgencyCode();
            var isSupremeUser = Context.User.IsSupremeUser();
            var role = Context.User.ExternalRole();
            var subRole = Context.User.SubRole();

            Logger.LogDebug("SiteMinder: {SiteMinder}", SiteMinder);
            Logger.LogDebug("authenticatedBySiteMinderPreviously: {AuthenticatedBySiteMinderPreviously}", authenticatedBySiteMinderPreviously);
            Logger.LogDebug("participantId : {ParticipantId}", participantId);
            Logger.LogDebug("agencyCode : {AgencyCode}", agencyCode);
            Logger.LogDebug("isSupremeUser : {IsSupremeUser}", isSupremeUser);
            Logger.LogDebug("role : {Role}", role);
            Logger.LogDebug("subRole : {SubRole}", subRole);
            if (!authenticatedBySiteMinderPreviously || role == null || subRole == null)
            {
                Logger.LogDebug("Not Authenticated through siteminder previously, checking against JCI");
                var request = new UserInfoRequest
                {
                    DeviceName = Environment.MachineName,
                    DomainUserGuid = siteMinderUserGuidHeader,
                    DomainUserId = Request.Headers["SM_USER"],
                    IpAddress = Request.Headers["X-Real-IP"],
                    TemporaryAccessGuid = ""
                };
                var jcUserInfo = await JCUserService.GetUserInfo(request);

                if (jcUserInfo == null)
                {
                    Logger.LogInformation("JCUserService Response == null");
                    return AuthenticateResult.Fail("Couldn't authenticate through JC-Interface.");
                }
                else
                {
                    Logger.LogDebug("JCUserServce Response is valid");
                }

                participantId = jcUserInfo.PartID;
                agencyCode = jcUserInfo.AgenID;
                role = jcUserInfo.RoleCd;
                subRole = jcUserInfo.SubRoleCd;
                isSupremeUser = true;
            }
            else
            {
                Logger.LogDebug("Authenticated by SiteMinder previously, all attributes are available");
            }

            var claims = new[] {
                new Claim(CustomClaimTypes.JcParticipantId, participantId),
                new Claim(CustomClaimTypes.JcAgencyCode, agencyCode),
                new Claim(CustomClaimTypes.ExternalRole, role),
                new Claim(CustomClaimTypes.SubRole, subRole),
                new Claim(CustomClaimTypes.IsSupremeUser, isSupremeUser.ToString()),
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);

            if (!authenticatedBySiteMinderPreviously)
            {
                Logger.LogDebug("Sign in with principal if not authenticated previously");
                await Context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            }
            else
            {
                Logger.LogDebug("Authenticated by SiteMinder previously");
            }

            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            Logger.LogInformation("Scheme.Name: {SchemeName}", Scheme.Name);
            Logger.LogInformation("Successfully logged in");
            return AuthenticateResult.Success(ticket);
        }
    }
}

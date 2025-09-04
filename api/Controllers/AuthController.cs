using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Infrastructure.Encryption;
using Scv.Api.Models.auth;
using Scv.Db.Models;
using Scv.Db.Models.Auth;

namespace Scv.Api.Controllers
{
    [Route("api/[controller]")]
    public class AuthController(ScvDbContext db, IConfiguration configuration, AesGcmEncryption aesGcmEncryption) : ControllerBase
    {
        public ScvDbContext Db { get; } = db;
        public IConfiguration Configuration { get; } = configuration;
        private AesGcmEncryption AesGcmEncryption { get; } = aesGcmEncryption;

        /// <summary>
        /// This cannot be called from AJAX or SWAGGER. It must be loaded in the browser location, because it brings the user to the SSO page. 
        /// </summary>
        /// <param name="redirectUri">URL to go back to.</param>
        [Authorize(AuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme)]
        [HttpGet("login")]
        public IActionResult Login(string redirectUri = "/api")
        {
            return Redirect(redirectUri);
        }

        /// <summary>
        /// Logout function, should wipe out all cookies. 
        /// </summary>
        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

            var logoutUrl = $"{Configuration.GetNonEmptyValue("Keycloak:Authority")}/protocol/openid-connect/logout";

            var forwardedHost = HttpContext.Request.Headers.ContainsKey("X-Forwarded-Host")
                ? HttpContext.Request.Headers["X-Forwarded-Host"].ToString()
                : Request.Host.ToString();
            var forwardedPort = HttpContext.Request.Headers["X-Forwarded-Port"];

            //We are always sending X-Forwarded-Port, only time we aren't is when we are hitting the API directly. 
            var baseUri = HttpContext.Request.Headers.ContainsKey("X-Forwarded-Host") ? $"{HttpContext.Request.Headers["X-Base-Href"]}" : "/api";

            var applicationUrl = $"{XForwardedForHelper.BuildUrlString(forwardedHost, forwardedPort, baseUri)}";
            var keycloakLogoutUrl = $"{logoutUrl}?post_logout_redirect_uri={applicationUrl}"; //TODO: add id_token_hint using id_token, currently removed in AuthenticationServiceCollectionExtension
            var siteMinderLogoutUrl = $"{Configuration.GetNonEmptyValue("SiteMinderLogoutUrl")}?returl={keycloakLogoutUrl}&retnow=1";
            return Redirect(siteMinderLogoutUrl);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("request-civil-file-access")]
        public async Task<IActionResult> RequestCivilFileAccess([FromBody] RequestCivilFileAccess request)
        {
            if (string.IsNullOrEmpty(request.FileId) || string.IsNullOrEmpty(request.UserId))
                return BadRequest();

            if (!User.IsServiceAccountUser())
                return Forbid();

            var agencyId = string.IsNullOrEmpty(request.AgencyId) ? "" : AesGcmEncryption.Encrypt(request.AgencyId);
            var partId = string.IsNullOrEmpty(request.PartId) ? "" : AesGcmEncryption.Encrypt(request.PartId);

            var expiryMinutes = float.Parse(Configuration.GetNonEmptyValue("RequestCivilFileAccessMinutes"));
            await Db.RequestFileAccess.AddAsync(new RequestFileAccess
            {
                FileId = request.FileId,
                UserId = request.UserId,
                UserName = request.UserName,
                AgencyId = agencyId,
                PartId = partId,
                Requested = DateTimeOffset.UtcNow,
                Expires = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            });
            await Db.SaveChangesAsync();

            var forwardedHost = Request.Headers["X-Forwarded-Host"];
            var forwardedPort = Request.Headers["X-Forwarded-Port"];
            var baseUrl = Request.Headers["X-Base-Href"];

            return Ok(new
            {
                Url = XForwardedForHelper.BuildUrlString(
                    forwardedHost,
                    forwardedPort,
                    baseUrl,
                    $"civil-file/{request.FileId}",
                    "fromA2A=true")
            });
        }


        /// <summary>
        /// Provides a way for the front-end to get info about the user.
        /// </summary>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
        [HttpGet]
        [Route("info")]
        public Task<ActionResult> UserInfo()
        {
            string userType;
            if (HttpContext.User.IsIdirUser())
                userType = "idir";
            else if (HttpContext.User.IsVcUser())
                userType = "vc";
            else
                userType = "judiciary";

            return Task.FromResult<ActionResult>(Ok(new
            {
                Permissions = HttpContext.User.Permissions(),
                Roles = HttpContext.User.Roles(),
                UserType = userType,
                EnableArchive = false,
                ExternalRole = HttpContext.User.ExternalRole(),
                SubRole = HttpContext.User.SubRole(),
                IsSupremeUser = HttpContext.User.IsSupremeUser(),
                AgencyCode = HttpContext.User.AgencyCode(),
                UserId = HttpContext.User.UserId(),
                JudgeId = HttpContext.User.JudgeId(),
                Email = HttpContext.User.Email(),
                IsActive = HttpContext.User.IsActive(),
                DateTime.UtcNow
            }));
        }
    }
}

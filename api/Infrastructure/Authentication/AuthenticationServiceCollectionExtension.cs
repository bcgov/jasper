using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Encryption;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Services;
using Scv.Db.Models;

namespace Scv.Api.Infrastructure.Authentication
{
    public static class AuthenticationServiceCollectionExtension
    {
        public static IServiceCollection AddScvAuthentication(this IServiceCollection services,
            IWebHostEnvironment env, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "SCV";
                if (env.IsDevelopment())
                    options.Cookie.Name += ".Development";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return context.Response.CompleteAsync();
                    },
                    OnValidatePrincipal = async cookieCtx =>
                    {
                        if (cookieCtx.Principal.Identity.AuthenticationType ==
                            SiteMinderAuthenticationHandler.SiteMinder)
                            return;

                        var accessTokenExpiration = DateTimeOffset.Parse(cookieCtx.Properties.GetTokenValue("expires_at"));
                        var timeRemaining = accessTokenExpiration.Subtract(DateTimeOffset.UtcNow);
                        var refreshThreshold = TimeSpan.Parse(configuration.GetNonEmptyValue("TokenRefreshThreshold"));

                        if (timeRemaining > refreshThreshold)
                            return;

                        var refreshToken = cookieCtx.Properties.GetTokenValue("refresh_token");
                        var httpClientFactory = cookieCtx.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                        var httpClient = httpClientFactory.CreateClient(nameof(CookieAuthenticationEvents));
                        var response = await httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
                        {
                            Address = configuration.GetNonEmptyValue("Keycloak:Authority") + "/protocol/openid-connect/token",
                            ClientId = configuration.GetNonEmptyValue("Keycloak:Client"),
                            ClientSecret = configuration.GetNonEmptyValue("Keycloak:Secret"),
                            RefreshToken = refreshToken
                        });

                        if (response.IsError)
                        {
                            cookieCtx.RejectPrincipal();
                            await cookieCtx.HttpContext.SignOutAsync(CookieAuthenticationDefaults
                                .AuthenticationScheme);
                        }
                        else
                        {
                            var expiresInSeconds = response.ExpiresIn;
                            var updatedExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);
                            cookieCtx.Properties.UpdateTokenValue("expires_at", updatedExpiresAt.ToString());
                            cookieCtx.Properties.UpdateTokenValue("refresh_token", response.RefreshToken);

                            // Indicate to the cookie middleware that the cookie should be remade (since we have updated it)
                            cookieCtx.ShouldRenew = true;
                        }
                    }
                };
            }
            )
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = configuration.GetNonEmptyValue("Keycloak:Authority");
                options.ClientId = configuration.GetNonEmptyValue("Keycloak:Client");
                options.ClientSecret = configuration.GetNonEmptyValue("Keycloak:Secret");
                options.RequireHttpsMetadata = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.SaveTokens = true;
                options.CallbackPath = "/api/auth/signin-oidc";
                options.Scope.Add("groups");
                options.Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = context =>
                    {
                        context.Properties.Items.Remove(".Token.id_token");
                        context.Properties.Items.Remove(".Token.access_token");
                        context.Properties.Items[".TokenNames"] = "refresh_token;token_type;expires_at";
                        return Task.CompletedTask;
                    },
#pragma warning disable 1998
                    OnTokenValidated = async context =>
#pragma warning restore 1998
                    {
                        if (!(context.Principal.Identity is ClaimsIdentity identity)) return;

                        var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger("OnTokenValidated");
                        logger.LogInformation($"OpenIdConnect UserId - {context.Principal.ExternalUserId()} - logged in.");

                        //Cleanup keycloak claims, that are unused.
                        foreach (var claim in identity.Claims.WhereToList(c =>
                            !CustomClaimTypes.UsedKeycloakClaimTypes.Contains(c.Type)))
                            identity.RemoveClaim(claim);

                        var applicationCode = "SCV";
                        var partId = configuration.GetNonEmptyValue("Request:PartId");
                        var agencyId = configuration.GetNonEmptyValue("Request:AgencyIdentifierId");
                        var isSupremeUser = false;
                        if (context.Principal.IsVcUser())
                        {
                            var db = context.HttpContext.RequestServices.GetRequiredService<ScvDbContext>();
                            var userId = context.Principal.ExternalUserId();
                            var now = DateTimeOffset.UtcNow;
                            var fileAccess = await db.RequestFileAccess
                                .Where(r => r.UserId == userId && r.Expires > now)
                                .OrderByDescending(x => x.Id)
                                .FirstOrDefaultAsync();

                            if (fileAccess != null && !string.IsNullOrEmpty(fileAccess.PartId) && !string.IsNullOrEmpty(fileAccess.AgencyId))
                            {
                                logger.LogInformation($"UserId - {context.Principal.ExternalUserId()} - Using credentials passed in from A2A.");
                                var aesGcmEncryption = context.HttpContext.RequestServices.GetRequiredService<AesGcmEncryption>();
                                partId = aesGcmEncryption.Decrypt(fileAccess.PartId);
                                agencyId = aesGcmEncryption.Decrypt(fileAccess.AgencyId);
                                applicationCode = "A2A";
                            }
                        }
                        else if (context.Principal.IsIdirUser() && context.Principal.Groups().Contains("court-viewer-supreme"))
                        {
                            isSupremeUser = true;
                        }

                        var claims = new List<Claim>();
                        claims.AddRange(new[] {
                            new Claim(CustomClaimTypes.ApplicationCode, applicationCode),
                            new Claim(CustomClaimTypes.JcParticipantId, partId),
                            new Claim(CustomClaimTypes.JcAgencyCode, agencyId),
                            new Claim(CustomClaimTypes.IsSupremeUser, isSupremeUser.ToString()),
                        });

                        await OnPostAuthSuccess(configuration, context, logger, claims);

                        identity.AddClaims(claims);
                    },
                    OnRedirectToIdentityProvider = context =>
                    {
                        if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
                        {
                            if (!context.Request.Path.StartsWithSegments("/api/auth/login"))
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        }

                        context.ProtocolMessage.SetParameter("kc_idp_hint",
                            context.Request.Query["redirectUri"].ToString().Contains("fromA2A=true")
                                ? configuration.GetNonEmptyValue("Keycloak:VcIdpHint")
                                : "idir");

                        context.ProtocolMessage.SetParameter("pres_req_conf_id", configuration.GetNonEmptyValue("Keycloak:PresReqConfId"));
                        if (context.HttpContext.Request.Headers["X-Forwarded-Host"].Count > 0)
                        {
                            var forwardedHost = context.HttpContext.Request.Headers["X-Forwarded-Host"];
                            var forwardedPort = context.HttpContext.Request.Headers["X-Forwarded-Port"];
                            var baseUrl = context.HttpContext.Request.Headers["X-Base-Href"];
                            context.ProtocolMessage.RedirectUri = XForwardedForHelper.BuildUrlString(
                                forwardedHost,
                                forwardedPort,
                                baseUrl,
                                options.CallbackPath);
                        }
                        return Task.CompletedTask;
                    }
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var key = Encoding.ASCII.GetBytes(configuration.GetNonEmptyValue("Keycloak:Secret"));
                options.Authority = configuration.GetNonEmptyValue("Keycloak:Authority");
                options.Audience = configuration.GetNonEmptyValue("Keycloak:Audience");
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.FromSeconds(5)
                };
                if (key.Length > 0) options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(key);
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.NoResult();
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        context.NoResult();
                        context.Response.StatusCode = 403;
                        return Task.CompletedTask;
                    }
                };
            })
            .AddScheme<AuthenticationSchemeOptions, SiteMinderAuthenticationHandler>(
                SiteMinderAuthenticationHandler.SiteMinder, null);

            return services;
        }

        private static async Task OnPostAuthSuccess(
            IConfiguration configuration,
            Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context,
            ILogger logger,
            List<Claim> claims)
        {
            var judgeId = configuration.GetNonEmptyValue("PCSS:JudgeId");
            var homeLocationId = configuration.GetNonEmptyValue("PCSS:JudgeHomeLocationId");

            // Default the user to his own external Judge Id.
            if (!string.IsNullOrWhiteSpace(context.Principal.ExternalJudgeId()))
            {
                var dashboardService = context.HttpContext.RequestServices.GetRequiredService<IDashboardService>();
                var judges = await dashboardService.GetJudges();

                // Find out the judge home location from list of judges
                int.TryParse(context.Principal.ExternalJudgeId(), out int externalJudgeId);

                var judge = judges.FirstOrDefault(j => j.PersonId == externalJudgeId);
                if (judge != null)
                {
                    judgeId = externalJudgeId.ToString();
                    homeLocationId = judge.HomeLocationId.ToString();
                }
            }

            // Remove checking when the "real" mongo db has been configured
            var connectionString = configuration.GetValue<string>("MONGODB_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Defaults the logged in user to the default judge.
                logger.LogInformation("Acting as Judge Id - {JudgeId} with Home Location Id - {HomeLocationId}.", judgeId, homeLocationId);
                claims.Add(new Claim(CustomClaimTypes.JudgeId, judgeId));
                claims.Add(new Claim(CustomClaimTypes.JudgeHomeLocationId, homeLocationId));
                return;
            }

            try
            {
                var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                var userDto = await userService.GetWithPermissionsAsync(context.Principal.Email());

                // Insert new user to the db. In the future, we need to ensure that the currently
                // logged on user has an account in PCSS before it is added to the db.
                if (userDto == null)
                {
                    userDto = new UserDto
                    {
                        FirstName = context.Principal.FindFirstValue(ClaimTypes.GivenName),
                        LastName = context.Principal.FindFirstValue(ClaimTypes.Surname),
                        Email = context.Principal.Email(),
                        UserGuid = context.Principal.UserGuid(),
                        IsActive = false
                    };

                    var result = await userService.AddAsync(userDto);
                    if (!result.Succeeded)
                    {
                        logger.LogInformation("Unable to add user: {Message}", result.Errors);
                    }

                    logger.LogInformation("A user has been added to the db.");

                    userDto = result.Payload;
                }

                // Attempt to override the default Judge Id and HomeLocationId if the current user has been mapped.
                if (userDto.JudgeId != null)
                {
                    var dashboardService = context.HttpContext.RequestServices.GetRequiredService<IDashboardService>();

                    var judges = await dashboardService.GetJudges();

                    var judge = judges.FirstOrDefault(j => j.PersonId == userDto.JudgeId);
                    if (judge != null)
                    {
                        judgeId = judge.PersonId.ToString();
                        homeLocationId = judge.HomeLocationId.ToString();
                    }
                }

                // UserId's value refers to the id in the User collection from MongoDb.
                claims.Add(new Claim(CustomClaimTypes.UserId, userDto.Id));

                // Add Roles and Permissions as claims if available
                var permissionsClaims = userDto.Permissions.Select(p => new Claim(CustomClaimTypes.Permission, p));
                claims.AddRange(permissionsClaims);

                var rolesClaims = userDto.Roles.Select(r => new Claim(CustomClaimTypes.Role, r));
                claims.AddRange(rolesClaims);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Something went wrong during post authentication process: {Exception}", ex);
            }
            finally
            {
                // Add the final value of judgeId and homeLocationId as claims of the current user
                logger.LogInformation("Acting as Judge Id - {JudgeId} with Home Location Id - {HomeLocationId}.", judgeId, homeLocationId);
                claims.Add(new Claim(CustomClaimTypes.JudgeId, judgeId));
                claims.Add(new Claim(CustomClaimTypes.JudgeHomeLocationId, homeLocationId));
            }
        }
    }
}

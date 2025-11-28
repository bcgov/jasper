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
using Newtonsoft.Json;
using PCSSCommon.Clients.AuthorizationServices;
using PCSSCommon.Models;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Encryption;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Services;
using Scv.Db.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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

                        var partId = configuration.GetNonEmptyValue("Request:PartId");
                        var agencyId = configuration.GetNonEmptyValue("Request:AgencyIdentifierId");
                        var isSupremeUser = false;

                        if (context.Principal.IsIdirUser() && context.Principal.Groups().Contains("court-viewer-supreme"))
                        {
                            isSupremeUser = true;
                        }

                        var claims = new List<Claim>();
                        claims.AddRange(new[] {
                            new Claim(CustomClaimTypes.JcParticipantId, partId),
                            new Claim(CustomClaimTypes.JcAgencyCode, agencyId),
                            new Claim(CustomClaimTypes.IsSupremeUser, isSupremeUser.ToString()),
                        });

                        await OnPostAuthSuccess(configuration, context, logger, claims);

                        identity.AddClaims(claims);
                    },
                    OnRedirectToIdentityProvider = context =>
                    {
                        if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication && !context.Request.Path.StartsWithSegments("/api/auth/login"))
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            context.HandleResponse();
                            return Task.CompletedTask;
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
            PersonSearchItem judge = null;
            try
            {
                if (int.TryParse(context.Principal.ExternalJudgeId(), out int externalJudgeId))
                {
                    judge = await GetJudgeById(context, externalJudgeId);
                }
                else
                {
                    logger.LogWarning("Failed to parse external judge id: {ExternalJudgeId}", context.Principal.ExternalJudgeId());
                }

                if (IsMongoDbConfigured(configuration))
                {
                    var userDto = await HandleMongoDbUser(context, logger, claims);

                    if (userDto?.JudgeId.HasValue == true)
                    {
                        judge = (await GetJudgeById(context, userDto.JudgeId.Value)) ?? judge; // Attempt to override the default Judge Id and HomeLocationId if the current user has been mapped.
                    }
                }
            }
            finally
            {
                logger.LogDebug("Logged user in, with potential fallback values judge person id: {JudgePersonId}, judge id: {JudgeId} judge home location id: {JudgeLocationId} homeLocationId: {HomeLocationId}", judge?.PersonId, judgeId, judge?.HomeLocationId, homeLocationId);
                judgeId = judge?.PersonId.ToString() ?? judgeId;
                homeLocationId = judge?.HomeLocationId.ToString() ?? homeLocationId;
                AddDefaultJudgeClaims(logger, claims, judgeId, homeLocationId);
            }
        }

        private static async Task<UserDto> HandleMongoDbUser(
            Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context,
            ILogger logger,
            List<Claim> claims)
        {
            UserDto userDto = null;
            try
            {
                var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                userDto = await userService.GetWithPermissionsAsync(context.Principal.Email());
                if (userDto == null)
                {
                    var newUser = await BuildNewUser(context, logger);
                    logger.LogInformation("Creating new user in SCV database: {NewUser}", JsonConvert.SerializeObject(newUser));
                    var result = await userService.AddAsync(newUser);
                    if (result.Payload == null || result.Errors.Any())
                    {
                        logger.LogError("Error creating new user in SCV database: {Errors}", string.Join(", ", result.Errors));
                        return null;
                    }
                    userDto = await userService.GetByIdWithPermissionsAsync(result.Payload.Id); // re-fetch to get permissions and roles;
                }
                AddUserClaims(claims, userDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Something went wrong during post authentication process: {Exception}", ex);
            }
            return userDto;
        }

        private static async Task<UserDto> BuildNewUser(
            Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context,
            ILogger logger)
        {
            UserDto userDto = new()
            {
                FirstName = context.Principal.FindFirstValue(ClaimTypes.GivenName),
                LastName = context.Principal.FindFirstValue(ClaimTypes.Surname),
                Email = context.Principal.Email(),
                UserGuid = context.Principal.IdirUserGuid(),
                NativeGuid = context.Principal.ProvjudUserGuid(),
                IsActive = false,
            };

            // If there is no ProvJud GUID, we cannot map this user to PCSS, so return early.
            if (userDto.NativeGuid == null)
            {
                return userDto;
            }

            var pcssAuthServiceClient = context.HttpContext.RequestServices.GetRequiredService<AuthorizationServicesClient>();
            var authorizationService = context.HttpContext.RequestServices.GetRequiredService<AuthorizationService>();
            var groupService = context.HttpContext.RequestServices.GetRequiredService<IGroupService>();

            UserItem matchingUser = null;
            try
            {
                logger.LogInformation("Requesting user information from PCSS.");
                var pcssUsers = await pcssAuthServiceClient.GetUsersAsync();
                matchingUser = pcssUsers?.FirstOrDefault(u => u.GUID == userDto.NativeGuid);
                logger.LogDebug("PCSS user lookup by GUID {UserGuid} returned: {MatchingUser}",
                    context.Principal.IdirUserGuid(),
                    matchingUser != null ? JsonConvert.SerializeObject(matchingUser) : "No match");

                if (matchingUser == null || !matchingUser.UserId.HasValue)
                {
                    return userDto;
                }

                var roleNameResult = await authorizationService.GetPcssUserRoleNames(matchingUser.UserId.Value);
                if (roleNameResult == null || roleNameResult.Errors.Any())
                {
                    return userDto;
                }

                var groupResult = await groupService.GetGroupsByAliases(roleNameResult.Payload);
                if (groupResult == null || groupResult.Errors.Any())
                {
                    return userDto;
                }

                var groupIds = groupResult.Payload.Select(g => g.Id).ToList();
                userDto.GroupIds = groupIds;
                userDto.IsActive = groupIds.Count > 0;
                if (matchingUser.UserId.HasValue)
                {
                    var judge = await GetJudgeByUserId(context, matchingUser.UserId.Value);
                    userDto.JudgeId = judge.PersonId;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unable to get user or groups from PCSS.");
            }

            return userDto;
        }

        private static async Task<PersonSearchItem> GetJudgeById(Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context, int judgeId)
        {
            var dashboardService = context.HttpContext.RequestServices.GetRequiredService<IDashboardService>();
            var judges = await dashboardService.GetJudges();

            return judges.FirstOrDefault(j => j.PersonId == judgeId);
        }

        private static async Task<PersonSearchItem> GetJudgeByUserId(Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context, int userId)
        {
            var dashboardService = context.HttpContext.RequestServices.GetRequiredService<IDashboardService>();
            var judges = await dashboardService.GetJudges();

            return judges.FirstOrDefault(j => j.UserId == userId);
        }

        private static bool IsMongoDbConfigured(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("MONGODB_CONNECTION_STRING");
            return !string.IsNullOrWhiteSpace(connectionString);
        }

        private static void AddDefaultJudgeClaims(ILogger logger, List<Claim> claims, string judgeId, string homeLocationId)
        {
            logger.LogInformation("Acting as Judge Id - {JudgeId} with Home Location Id - {HomeLocationId}.", judgeId, homeLocationId);
            claims.Add(new Claim(CustomClaimTypes.JudgeId, judgeId));
            claims.Add(new Claim(CustomClaimTypes.JudgeHomeLocationId, homeLocationId));
        }

        private static void AddUserClaims(List<Claim> claims, UserDto userDto)
        {
            claims.Add(new Claim(CustomClaimTypes.UserId, userDto.Id));
            claims.Add(new Claim(CustomClaimTypes.IsActive, userDto.IsActive.ToString()));

            claims.AddRange(userDto.Permissions.Select(p => new Claim(CustomClaimTypes.Permission, p)));
            claims.AddRange(userDto.Roles.Select(r => new Claim(CustomClaimTypes.Role, r)));
            claims.AddRange(userDto.GroupIds.Select(g => new Claim(CustomClaimTypes.Groups, g)));
        }
    }
}

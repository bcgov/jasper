using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PCSSCommon.Models;
using Polly;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Services;
using Scv.Core.Helpers;
using Scv.Core.Helpers.Extensions;
using Scv.Models.AccessControlManagement;

namespace Scv.Api.Infrastructure.Authentication
{
    public static class AuthenticationServiceCollectionExtension
    {
        private const string PcssAuthorizationFailure = "Unable to establish JASPER access from PCSS.";
        private const string AuthErrorParameter = "authError";
        private const string PcssAuthError = "pcss_authorization_failed";
        private const string GenericAuthError = "authentication_failed";
        private static readonly object PcssAuthorizationFailed = new();

        public static IServiceCollection AddScvAuthentication(this IServiceCollection services,
            IWebHostEnvironment env, IConfiguration configuration)
        {
            services.AddOptions<KeycloakOptions>()
                .Bind(configuration.GetSection("CsoKeycloak"))
                .ValidateDataAnnotations();

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

                        var accessTokenExpiration = DateTimeOffset.Parse(cookieCtx.Properties.GetTokenValue("expires_at"), CultureInfo.InvariantCulture);
                        var timeRemaining = accessTokenExpiration.Subtract(DateTimeOffset.UtcNow);
                        var refreshThreshold = TimeSpan.Parse(configuration.GetNonEmptyValue("TokenRefreshThreshold"), CultureInfo.InvariantCulture);

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
                            cookieCtx.Properties.UpdateTokenValue("expires_at", updatedExpiresAt.ToString(CultureInfo.InvariantCulture));
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
                    OnRemoteFailure = async context =>
                    {
                        var isPcssFailure = context.HttpContext.Items.ContainsKey(PcssAuthorizationFailed);
                        if (isPcssFailure)
                        {
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }

                        var error = isPcssFailure ? PcssAuthError : GenericAuthError;
                        context.HandleResponse();
                        context.Response.Redirect(QueryHelpers.AddQueryString(GetFrontendBasePath(context.Request), AuthErrorParameter, error));
                    },
#pragma warning disable 1998
                    OnTokenValidated = async context =>
#pragma warning restore 1998
                    {
                        if (context.Principal.Identity is not ClaimsIdentity identity) return;

                        var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger("OnTokenValidated");
                        logger.LogInformation("OpenIdConnect UserId - {ExternalUserId} - logged in.", context.Principal.ExternalUserId());

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
                        claims.AddRange([
                            new Claim(CustomClaimTypes.JcParticipantId, partId),
                            new Claim(CustomClaimTypes.JcAgencyCode, agencyId),
                            new Claim(CustomClaimTypes.IsSupremeUser, isSupremeUser.ToString()),
                        ]);

                        try
                        {
                            await OnPostAuthSuccess(configuration, context, logger, claims);
                        }
                        catch (Exception ex) when (RequiresPcssAuthorization(context.Principal))
                        {
                            logger.LogError(ex, "Unable to verify Provincial Court authorization during sign-in.");
                            FailPcssAuthorization(context);
                        }

                        if (context.Result == null)
                        {
                            identity.AddClaims(claims);
                        }
                    },
                    OnRedirectToIdentityProvider = context =>
                    {
                        if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication
                            && (!context.Request.Path.StartsWithSegments("/api/auth/login") || IsAuthFailureReturnUrl(context.Request)))
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
            .AddJwtBearer(CsoPolicies.AuthenticationScheme, options =>
            {
                var csoOptions = new KeycloakOptions();
                configuration.GetSection("CsoKeycloak").Bind(csoOptions);

                options.Authority = csoOptions.Authority;
                options.Audience = csoOptions.Audience;
                options.RequireHttpsMetadata = csoOptions.RequireHttpsMetadata;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = csoOptions.ValidateIssuer,
                    ValidIssuer = csoOptions.Authority,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = csoOptions.ValidateIssuer,
                    ValidAudience = csoOptions.Audience,
                    ClockSkew = TimeSpan.FromSeconds(5)
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
            PersonSearchItem judge = null;
            UserDto userDto = null;
            var usedStoredAuthorization = false;
            var isMongoDbConfigured = IsMongoDbConfigured(configuration);
            var usePcssSync = isMongoDbConfigured && RequiresPcssAuthorization(context.Principal);
            try
            {
                if (usePcssSync)
                {
                    (userDto, usedStoredAuthorization) = await HandleMongoDbUser(context, logger, claims);
                    if (usedStoredAuthorization || context.Result != null)
                    {
                        return;
                    }
                }

                if (int.TryParse(context.Principal.ExternalJudgeId(), out int externalJudgeId))
                {
                    judge = await GetJudgeById(context, externalJudgeId);
                }
                else
                {
                    logger.LogWarning("Failed to parse external judge id: {ExternalJudgeId}", context.Principal.ExternalJudgeId());
                }

                if (isMongoDbConfigured)
                {
                    if (!usePcssSync)
                    {
                        (userDto, _) = await HandleMongoDbUser(context, logger, claims);
                    }

                    if (userDto?.JudgeId.HasValue == true)
                    {
                        judge = (await GetJudgeById(context, userDto.JudgeId.Value)) ?? judge; // Attempt to override the default Judge Id and HomeLocationId if the current user has been mapped.
                    }
                }
            }
            finally
            {
                if (context.Result == null)
                {
                    var judgeId = usedStoredAuthorization
                        ? userDto.JudgeId.ToString()
                        : judge?.PersonId.ToString() ?? configuration.GetNonEmptyValue("PCSS:JudgeId");
                    var homeLocationId = judge?.HomeLocationId.ToString() ?? configuration.GetNonEmptyValue("PCSS:JudgeHomeLocationId");
                    logger.LogDebug("Logged user in with judge person id: {JudgePersonId}, judge home location id: {JudgeLocationId}",
                        usedStoredAuthorization ? userDto.JudgeId : judge?.PersonId, judge?.HomeLocationId);
                    AddDefaultJudgeClaims(logger, claims, judgeId, homeLocationId);
                }
            }
        }

        private static async Task<(UserDto User, bool UsedStoredAuthorization)> HandleMongoDbUser(
            Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context,
            ILogger logger,
            List<Claim> claims)
        {
            UserDto userDto = null;
            var useStoredAuthorization = false;
            var requiresPcss = RequiresPcssAuthorization(context.Principal);
            try
            {
                var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                var provjudUserGuid = context.Principal.ProvjudUserGuid();
                if (requiresPcss && provjudUserGuid == null)
                {
                    logger.LogInformation("provjud user unexpectedly missing guid - retrying. User {Email}", context.Principal.Email());
                    // The IDP mapper can populate the GUID after the initial login.
                    provjudUserGuid = await RefreshKeycloakUserInfoAsync(context, logger);
                    if (provjudUserGuid == null)
                    {
                        FailPcssAuthorization(context);
                        return default;
                    }
                }

                var email = context.Principal.Email();
                if (requiresPcss)
                {
                    userDto = await userService.GetForPcssSyncAsync(provjudUserGuid, email);
                }
                else if (!string.IsNullOrWhiteSpace(email))
                {
                    userDto = await userService.GetWithPermissionsAsync(email);
                }

                var isNewUser = userDto == null;
                userDto ??= BuildNewUser(context, provjudUserGuid);
                var userId = userDto.Id;

                if (requiresPcss)
                {
                    var nativeGuidChanged = string.IsNullOrWhiteSpace(userDto.NativeGuid);
                    if (!nativeGuidChanged && userDto.NativeGuid != provjudUserGuid)
                    {
                        logger.LogWarning("User with keycloak guid returned mismatched guid from JASPER database. {NativeGuid} {ProvjudUserGuid}", userDto.NativeGuid, provjudUserGuid);
                        FailPcssAuthorization(context);
                        return default;
                    }

                    userDto.NativeGuid = provjudUserGuid;
                    var pcssSyncService = context.HttpContext.RequestServices.GetRequiredService<IPcssSyncService>();
                    logger.LogInformation("Synchronizing user {Email} with latest PCSS data", userDto.Email);
                    var syncResult = await pcssSyncService.UpdateUserFromPcss(userDto);
                    useStoredAuthorization = syncResult.Outcome == PcssSyncOutcome.Failed;
                    if (useStoredAuthorization && isNewUser)
                    {
                        FailPcssAuthorization(context);
                        return default;
                    }

                    if (!useStoredAuthorization && !isNewUser && (nativeGuidChanged || syncResult.HasChanges))
                    {
                        var updateResult = await userService.UpdateFromPcssAsync(userDto);
                        if (!updateResult.Succeeded)
                        {
                            logger.LogError("Error saving user in JASPER database: {Errors}", string.Join(", ", updateResult.Errors));
                            FailPcssAuthorization(context);
                            return default;
                        }
                    }
                }

                if (isNewUser)
                {
                    logger.LogInformation("Creating new user in JASPER database: {NewUser}", JsonConvert.SerializeObject(userDto));
                    var result = await userService.AddAsync(userDto);
                    if (!result.Succeeded || result.Payload == null)
                    {
                        logger.LogError("Error saving user in JASPER database: {Errors}", string.Join(", ", result.Errors));
                        if (requiresPcss)
                        {
                            FailPcssAuthorization(context);
                        }
                        return default;
                    }
                    userId = result.Payload.Id;
                }

                if (isNewUser || requiresPcss)
                {
                    userDto = await userService.GetByIdWithPermissionsAsync(userId);
                }

                if (requiresPcss && userDto == null)
                {
                    logger.LogError("Unable to reload the JASPER user after synchronization with PCSS.");
                    FailPcssAuthorization(context);
                    return default;
                }

                if (useStoredAuthorization)
                {
                    if (!userDto.HasJasperAccess())
                    {
                        FailPcssAuthorization(context);
                        return default;
                    }

                    logger.LogWarning("PCSS synchronization failed for user {UserId}. Continuing sign-in with stored JASPER authorization.",
                        userId.SanitizeForLog());
                }

                AddUserClaims(claims, userDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Something went wrong during post authentication process: {Exception}", ex);
                if (requiresPcss)
                {
                    FailPcssAuthorization(context);
                    return default;
                }
            }
            return (userDto, useStoredAuthorization);
        }

        private static UserDto BuildNewUser(
            Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context,
            string provjudUserGuid)
        {
            return new UserDto
            {
                FirstName = context.Principal.FindFirstValue(ClaimTypes.GivenName),
                LastName = context.Principal.FindFirstValue(ClaimTypes.Surname),
                Email = context.Principal.Email(),
                UserGuid = context.Principal.IdirUserGuid(),
                NativeGuid = provjudUserGuid,
                IsActive = false,
            };
        }

        private static async Task<PersonSearchItem> GetJudgeById(Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context, int judgeId)
        {
            var judgeService = context.HttpContext.RequestServices.GetRequiredService<IJudgeService>();
            var judges = await judgeService.GetJudges();

            return judges.FirstOrDefault(j => j.PersonId == judgeId);
        }

        private static bool IsMongoDbConfigured(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("MONGODB_CONNECTION_STRING");
            return !string.IsNullOrWhiteSpace(connectionString);
        }

        private static void AddDefaultJudgeClaims(ILogger logger, List<Claim> claims, string judgeId, string homeLocationId)
        {
            logger.LogInformation("Acting as Judge Id - {JudgeId} with Home Location Id - {HomeLocationId}.", judgeId, homeLocationId);
            if (!string.IsNullOrEmpty(judgeId))
            {
                claims.Add(new Claim(CustomClaimTypes.JudgeId, judgeId));
            }
            if (!string.IsNullOrEmpty(homeLocationId))
            {
                claims.Add(new Claim(CustomClaimTypes.JudgeHomeLocationId, homeLocationId));
            }
        }

        private static void AddUserClaims(List<Claim> claims, UserDto userDto)
        {
            claims.Add(new Claim(CustomClaimTypes.UserId, userDto.Id));
            claims.Add(new Claim(CustomClaimTypes.IsActive, userDto.IsActive.ToString()));

            claims.AddRange(userDto.Permissions.Select(p => new Claim(CustomClaimTypes.Permission, p)));
            claims.AddRange(userDto.Roles.Select(r => new Claim(CustomClaimTypes.Role, r)));
            claims.AddRange(userDto.GroupIds.Select(g => new Claim(CustomClaimTypes.Groups, g)));
        }

        private static bool RequiresPcssAuthorization(ClaimsPrincipal user)
            => user.HasClaim(claim => claim.Type == CustomClaimTypes.ProvjudUserGuid && !string.IsNullOrWhiteSpace(claim.Value))
                || (!user.IsIdirUser() && !user.IsVcUser()
                    && user.Email()?.EndsWith("@provincialcourt.bc.ca", StringComparison.OrdinalIgnoreCase) == true);

        private static void FailPcssAuthorization(Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context)
        {
            context.HttpContext.Items[PcssAuthorizationFailed] = true;
            context.Fail(PcssAuthorizationFailure);
        }

        private static string GetFrontendBasePath(HttpRequest request)
        {
            var path = request.PathBase.Value?.TrimEnd('/') ?? string.Empty;
            // The proxy supplies PathBase. Keep the redirect on this origin and reject ambiguous path segments.
            if (path.Length == 0 || path[0] != '/' || path[1..].Split('/').Any(segment =>
                segment.Length == 0 || segment is "." or ".."
                || segment.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '-' or '_'))))
            {
                return "/";
            }

            return path + "/";
        }

        private static bool IsAuthFailureReturnUrl(HttpRequest request)
        {
            // Older frontends immediately challenge again on a 401 during initialization.
            return Uri.TryCreate(request.Query["redirectUri"].ToString(), UriKind.Absolute, out var returnUrl)
                && returnUrl.AbsolutePath == GetFrontendBasePath(request)
                && QueryHelpers.ParseQuery(returnUrl.Query).TryGetValue(AuthErrorParameter, out var error)
                && (error == PcssAuthError || error == GenericAuthError);
        }

        private static async Task<string> RefreshKeycloakUserInfoAsync(
            Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context,
            ILogger logger)
        {
            if (context.Principal?.Identity is not ClaimsIdentity identity)
            {
                return null;
            }

            var accessToken = context.TokenEndpointResponse?.AccessToken
                ?? context.Properties.GetTokenValue("access_token");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                logger.LogWarning("Unable to refresh Keycloak userinfo because access token is missing.");
                return null;
            }

            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(OpenIdConnectEvents));
            var userInfoEndpoint = configuration.GetNonEmptyValue("Keycloak:Authority") + "/protocol/openid-connect/userinfo";

            var policy = Policy<UserInfoResponse>
                .Handle<Exception>()
                .OrResult(result => result.IsError)
                .WaitAndRetryAsync(
                    2,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    (outcome, delay, attempt, _) =>
                    {
                        var reason = outcome.Exception?.Message ?? outcome.Result?.Error;
                        logger.LogWarning(
                            "Retrying Keycloak userinfo request (attempt {Attempt}) after {Delay}s. Reason: {Reason}",
                            attempt,
                            delay.TotalSeconds,
                            reason);
                    });

            var response = await policy.ExecuteAsync(ct => httpClient.GetUserInfoAsync(
                new UserInfoRequest
                {
                    Address = userInfoEndpoint,
                    Token = accessToken
                }, ct),
                CancellationToken.None);

            if (response.IsError)
            {
                logger.LogWarning("Failed to refresh Keycloak userinfo: {Error}", response.Error);
                return null;
            }

            var provjudClaim = response.Claims.FirstOrDefault(claim => claim.Type == CustomClaimTypes.ProvjudUserGuid);
            if (provjudClaim == null)
            {
                return null;
            }

            foreach (var claim in identity.FindAll(CustomClaimTypes.ProvjudUserGuid).ToList())
            {
                identity.RemoveClaim(claim);
            }
            identity.AddClaim(provjudClaim);
            return context.Principal.ProvjudUserGuid();
        }
    }
}

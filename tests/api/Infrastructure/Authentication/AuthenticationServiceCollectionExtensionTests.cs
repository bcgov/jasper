using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Scv.Api.Infrastructure.Authentication;
using Xunit;

namespace tests.api.Infrastructure.Authentication;

public class AuthenticationServiceCollectionExtensionTests
{
    [Fact]
    public void OpenIdConnectUsesTwentyMinutesForNonceAndCorrelation()
    {
        var options = GetOptions();
        var issuedAt = DateTimeOffset.UtcNow;
        var expectedExpiry = issuedAt.AddMinutes(20);
        var context = new DefaultHttpContext();

        Assert.Equal(TimeSpan.FromMinutes(20), options.ProtocolValidator.NonceLifetime);
        Assert.Equal(TimeSpan.FromMinutes(20), options.RemoteAuthenticationTimeout);
        Assert.Equal(expectedExpiry, options.NonceCookie.Build(context, issuedAt).Expires);
        Assert.Equal(expectedExpiry, options.CorrelationCookie.Build(context, issuedAt).Expires);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(17)]
    public void TokenResponseAcceptsMatchingNonceWithinWindow(int nonceAgeMinutes)
    {
        var nonce = CreateNonce(nonceAgeMinutes);
        GetOptions().ProtocolValidator.ValidateTokenResponse(CreateTokenResponse(nonce, nonce));
    }

    [Theory]
    [InlineData(21, "matching")]
    [InlineData(5, null)]
    [InlineData(5, "different")]
    public void TokenResponseRejectsExpiredMissingAndMismatchedNonce(int nonceAgeMinutes, string cookieNonce)
    {
        var nonce = CreateNonce(nonceAgeMinutes);
        var validationContext = CreateTokenResponse(nonce, cookieNonce == "matching" ? nonce : cookieNonce);

        Assert.Throws<OpenIdConnectProtocolInvalidNonceException>(() =>
            GetOptions().ProtocolValidator.ValidateTokenResponse(validationContext));
    }

    private static OpenIdConnectOptions GetOptions()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Keycloak:Authority"] = "https://identity.example.test",
            ["Keycloak:Client"] = "jasper-test",
            ["Keycloak:Secret"] = "test-secret"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScvAuthentication(Mock.Of<IWebHostEnvironment>(), configuration);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);
    }

    private static string CreateNonce(int ageMinutes) =>
        DateTime.UtcNow.AddMinutes(-ageMinutes).ToBinary().ToString(CultureInfo.InvariantCulture) + ".test-value";

    private static OpenIdConnectProtocolValidationContext CreateTokenResponse(string nonce, string cookieNonce)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken("https://identity.example.test", "jasper-test",
        [
            new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
            new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Nonce, nonce)
        ], now.AddMinutes(-1), now.AddMinutes(10));

        return new OpenIdConnectProtocolValidationContext
        {
            ClientId = "jasper-test",
            Nonce = cookieNonce,
            ProtocolMessage = new OpenIdConnectMessage { IdToken = "id-token", AccessToken = "access-token" },
            ValidatedIdToken = token
        };
    }
}

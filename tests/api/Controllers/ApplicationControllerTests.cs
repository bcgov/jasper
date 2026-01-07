using System.Collections.Generic;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Scv.Api.Controllers;
using Scv.Core.Helpers.Exceptions;
using Xunit;

namespace tests.api.Controllers;

public class ApplicationControllerTests
{
    private readonly Faker _faker;

    public ApplicationControllerTests()
    {
        _faker = new Faker();
    }

    private static ApplicationController CreateController(Dictionary<string, string> configValues)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
        return new ApplicationController(configuration);
    }

    #region Unit Tests

    [Fact]
    public void GetApplicationInfo_ThrowsConfigurationException_WhenNutrientFeLicenseKeyIsNull()
    {
        var expectedEnvironment = _faker.PickRandom("Development", "Staging");
        
        var configValues = new Dictionary<string, string>
        {
            // NUTRIENT_FE_LICENSE_KEY is intentionally missing
            ["ASPNETCORE_ENVIRONMENT"] = expectedEnvironment
        };

        var controller = CreateController(configValues);

        var exception = Assert.Throws<ConfigurationException>(() => controller.GetApplicationInfo());
        Assert.Equal("Configuration 'NUTRIENT_FE_LICENSE_KEY' is invalid or missing.", exception.Message);
    }

    [Fact]
    public void GetApplicationInfo_ThrowsConfigurationException_WhenNutrientFeLicenseKeyIsEmpty()
    {
        var expectedEnvironment = _faker.PickRandom("Development", "Staging");
        
        var configValues = new Dictionary<string, string>
        {
            ["NUTRIENT_FE_LICENSE_KEY"] = string.Empty,
            ["ASPNETCORE_ENVIRONMENT"] = expectedEnvironment
        };

        var controller = CreateController(configValues);

        var exception = Assert.Throws<ConfigurationException>(() => controller.GetApplicationInfo());
        Assert.Equal("Configuration 'NUTRIENT_FE_LICENSE_KEY' is invalid or missing.", exception.Message);
    }

    [Fact]
    public void GetApplicationInfo_ThrowsConfigurationException_WhenEnvironmentIsNull()
    {
        var expectedLicenseKey = _faker.Random.AlphaNumeric(32);
        
        var configValues = new Dictionary<string, string>
        {
            ["NUTRIENT_FE_LICENSE_KEY"] = expectedLicenseKey
            // ASPNETCORE_ENVIRONMENT is intentionally missing
        };

        var controller = CreateController(configValues);

        var exception = Assert.Throws<ConfigurationException>(() => controller.GetApplicationInfo());
        Assert.Equal("Configuration 'ASPNETCORE_ENVIRONMENT' is invalid or missing.", exception.Message);
    }

    [Fact]
    public void GetApplicationInfo_ThrowsConfigurationException_WhenEnvironmentIsEmpty()
    {
        var expectedLicenseKey = _faker.Random.AlphaNumeric(32);
        
        var configValues = new Dictionary<string, string>
        {
            ["NUTRIENT_FE_LICENSE_KEY"] = expectedLicenseKey,
            ["ASPNETCORE_ENVIRONMENT"] = string.Empty
        };

        var controller = CreateController(configValues);

        var exception = Assert.Throws<ConfigurationException>(() => controller.GetApplicationInfo());
        Assert.Equal("Configuration 'ASPNETCORE_ENVIRONMENT' is invalid or missing.", exception.Message);
    }

    #endregion

    #region Controller Attribute Tests

    [Fact]
    public void ApplicationController_HasCorrectAuthorizeAttribute()
    {
        var controllerType = typeof(ApplicationController);
        var authorizeAttributes = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);

        Assert.NotEmpty(authorizeAttributes);
        var authorizeAttribute = authorizeAttributes[0] as Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("SiteMinder, OpenIdConnect", authorizeAttribute.AuthenticationSchemes);
        Assert.Equal("ProviderAuthorizationHandler", authorizeAttribute.Policy);
    }

    [Fact]
    public void ApplicationController_HasCorrectRouteAttribute()
    {
        var controllerType = typeof(ApplicationController);
        var routeAttributes = controllerType.GetCustomAttributes(typeof(RouteAttribute), false);

        Assert.NotEmpty(routeAttributes);
        var routeAttribute = routeAttributes[0] as RouteAttribute;
        Assert.NotNull(routeAttribute);
        Assert.Equal("api/[controller]", routeAttribute.Template);
    }

    [Fact]
    public void ApplicationController_HasApiControllerAttribute()
    {
        var controllerType = typeof(ApplicationController);
        var apiControllerAttributes = controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false);

        Assert.NotEmpty(apiControllerAttributes);
    }

    [Fact]
    public void GetApplicationInfo_HasCorrectHttpGetAttribute()
    {
        var method = typeof(ApplicationController).GetMethod("GetApplicationInfo");
        var httpGetAttributes = method.GetCustomAttributes(typeof(HttpGetAttribute), false);

        Assert.NotEmpty(httpGetAttributes);
        var httpGetAttribute = httpGetAttributes[0] as HttpGetAttribute;
        Assert.NotNull(httpGetAttribute);
        Assert.Equal("info", httpGetAttribute.Template);
    }

    [Fact]
    public void GetApplicationInfo_HasCorrectProducesResponseTypeAttribute()
    {
        var method = typeof(ApplicationController).GetMethod("GetApplicationInfo");
        var producesResponseTypeAttributes = method.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), false);

        Assert.NotEmpty(producesResponseTypeAttributes);
        var producesResponseTypeAttribute = producesResponseTypeAttributes[0] as ProducesResponseTypeAttribute;
        Assert.NotNull(producesResponseTypeAttribute);
        Assert.Equal(200, producesResponseTypeAttribute.StatusCode);
    }

    #endregion
}
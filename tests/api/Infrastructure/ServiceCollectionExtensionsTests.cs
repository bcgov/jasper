using System.Collections.Generic;
using System.Linq;
using GdPicture14;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scv.Api.Documents;
using Scv.Api.Infrastructure;
using Xunit;

namespace tests.api.Infrastructure;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNutrient_RegistersPdfMergePreparationServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string>("NUTRIENT_BE_LICENSE_KEY", string.Empty)
            ])
            .Build();

        services.AddNutrient(configuration);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IPdfMergePreparationStrategyResolver)
                && descriptor.ImplementationType == typeof(PdfMergePreparationStrategyResolver)
                && descriptor.Lifetime == ServiceLifetime.Scoped);

        var strategyRegistrations = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPdfMergePreparationStrategy))
            .ToList();

        Assert.Contains(
            strategyRegistrations,
            descriptor => descriptor.ImplementationType == typeof(TransitoryDocumentPdfMergePreparationStrategy)
                && descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(
            strategyRegistrations,
            descriptor => descriptor.ImplementationType == typeof(DefaultPdfMergePreparationStrategy)
                && descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Scv.Api.Infrastructure.Options;

namespace Scv.Api.Documents.Conversion;

public enum DocumentPdfConverterId
{
    OfficeIMO = 0,
    LibreOffice = 1,
    Magick = 2
}

internal interface IDocumentPdfRouteTable
{
    IReadOnlyList<IDocumentPdfConversionStrategy> GetStrategies(DocumentFormat format);
}

internal sealed class DocumentPdfRouteTable : IDocumentPdfRouteTable, IHostedService
{
    private readonly IReadOnlyDictionary<DocumentFormatFamily, IReadOnlyList<IDocumentPdfConversionStrategy>> _routes;

    public DocumentPdfRouteTable(
        IOptions<DocumentConversionOptions> options,
        IEnumerable<IDocumentPdfConversionStrategy> strategies)
    {
        var errors = new List<string>();
        var registry = strategies
            .GroupBy(strategy => strategy.Id)
            .ToDictionary(group => group.Key, group => group.ToArray());
        foreach (var duplicate in registry.Where(item => item.Value.Length != 1))
        {
            errors.Add($"Document converter '{duplicate.Key}' is registered {duplicate.Value.Length} times.");
        }

        _routes = DocumentConversionRouting.ParseRoutes(options.Value.Routes).ToDictionary(
            route => route.Key,
            route => (IReadOnlyList<IDocumentPdfConversionStrategy>)route.Value
                .Select(converterId => ResolveStrategy(route.Key, converterId, registry, errors))
                .Where(strategy => strategy is not null)
                .ToArray());

        if (errors.Count > 0)
        {
            throw new OptionsValidationException(
                DocumentConversionOptions.SectionName,
                typeof(DocumentConversionOptions),
                errors);
        }
    }

    public IReadOnlyList<IDocumentPdfConversionStrategy> GetStrategies(DocumentFormat format)
    {
        var family = DocumentFormatDetector.GetFamily(format);
        if (family is null || !_routes.TryGetValue(family.Value, out var route))
        {
            return [];
        }

        return route;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IDocumentPdfConversionStrategy ResolveStrategy(
        DocumentFormatFamily family,
        DocumentPdfConverterId converterId,
        IReadOnlyDictionary<DocumentPdfConverterId, IDocumentPdfConversionStrategy[]> registry,
        ICollection<string> errors)
    {
        if (!registry.TryGetValue(converterId, out var registrations) || registrations.Length != 1)
        {
            errors.Add($"DocumentConversion route '{family}' references unregistered converter '{converterId}'.");
            return null;
        }

        var strategy = registrations[0];
        foreach (var format in DocumentFormatDetector.GetFormats(family).Where(format => !strategy.CanConvert(format)))
        {
            errors.Add($"DocumentConversion converter '{converterId}' does not support format '{format}' in route '{family}'.");
        }

        return strategy;
    }
}

internal static class DocumentConversionRouting
{
    internal static IReadOnlyDictionary<DocumentFormatFamily, IReadOnlyList<DocumentPdfConverterId>> ParseRoutes(
        IReadOnlyDictionary<string, List<string>> configuredRoutes)
    {
        return configuredRoutes.ToDictionary(
            route => Enum.Parse<DocumentFormatFamily>(route.Key, ignoreCase: true),
            route => (IReadOnlyList<DocumentPdfConverterId>)route.Value
                .Select(value => Enum.Parse<DocumentPdfConverterId>(value, ignoreCase: true))
                .ToArray());
    }

}

internal sealed class DocumentConversionOptionsValidator : IValidateOptions<DocumentConversionOptions>
{
    public ValidateOptionsResult Validate(string name, DocumentConversionOptions options)
    {
        var errors = new List<string>();
        ValidateExtensions(options.DefaultEnabledExtensions, errors);
        if (options.Routes is null || options.Routes.Count == 0)
        {
            errors.Add("DocumentConversion:Routes must not be empty.");
        }
        else
        {
            foreach (var (configuredFamily, configuredRoute) in options.Routes)
            {
                ValidateRoute(configuredFamily, configuredRoute, errors);
            }

            ValidateRouteCoverage(options, errors);
        }

        if (options.LibreOfficeProcessTimeoutSeconds >= options.OverallConversionTimeoutSeconds)
        {
            errors.Add("LibreOfficeProcessTimeoutSeconds must be less than OverallConversionTimeoutSeconds.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateExtensions(
        IReadOnlyCollection<string> configuredExtensions,
        ICollection<string> errors)
    {
        if (configuredExtensions is null || configuredExtensions.Count == 0)
        {
            errors.Add("DocumentConversion:DefaultEnabledExtensions must not be empty.");
            return;
        }

        var normalizedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var configuredExtension in configuredExtensions)
        {
            var extension = DocumentPdfConversionService.NormalizeExtension(configuredExtension);
            if (extension is null || !DocumentFormatDetector.TryGetFormatForExtension(extension, out _))
            {
                errors.Add($"DocumentConversion extension '{configuredExtension}' is not registered.");
            }
            else if (!normalizedExtensions.Add(extension))
            {
                errors.Add($"DocumentConversion extension '{configuredExtension}' is duplicated.");
            }
        }
    }

    private static void ValidateRouteCoverage(
        DocumentConversionOptions options,
        ICollection<string> errors)
    {
        var configuredFamilies = options.Routes.Keys
            .Select(value => Enum.TryParse<DocumentFormatFamily>(value, ignoreCase: true, out var family)
                ? family
                : (DocumentFormatFamily?)null)
            .Where(family => family.HasValue)
            .Select(family => family.Value)
            .ToHashSet();

        foreach (var configuredExtension in options.DefaultEnabledExtensions)
        {
            var extension = DocumentPdfConversionService.NormalizeExtension(configuredExtension);
            if (extension is null ||
                !DocumentFormatDetector.TryGetFormatForExtension(extension, out var format))
            {
                continue;
            }

            var family = DocumentFormatDetector.GetFamily(format);
            if (family.HasValue && !configuredFamilies.Contains(family.Value))
            {
                errors.Add($"DocumentConversion extension '{extension}' has no route for family '{family}'.");
            }
        }
    }

    private static void ValidateRoute(
        string configuredFamily,
        IReadOnlyCollection<string> configuredRoute,
        ICollection<string> errors)
    {
        if (!Enum.TryParse<DocumentFormatFamily>(configuredFamily, ignoreCase: true, out var family) ||
            !Enum.IsDefined(family))
        {
            errors.Add($"DocumentConversion route '{configuredFamily}' is not a registered format family.");
            return;
        }

        if (configuredRoute is null || configuredRoute.Count == 0)
        {
            errors.Add($"DocumentConversion route '{family}' must provide at least one converter.");
            return;
        }

        var parsed = configuredRoute
            .Select(converterName => ParseConverter(converterName, family, errors))
            .Where(converter => converter.HasValue)
            .Select(converter => converter.Value)
            .ToArray();
        if (parsed.Length != parsed.Distinct().Count())
        {
            errors.Add($"DocumentConversion route '{family}' contains duplicate converter identifiers.");
        }
    }

    private static DocumentPdfConverterId? ParseConverter(
        string converterName,
        DocumentFormatFamily family,
        ICollection<string> errors)
    {
        if (!Enum.TryParse<DocumentPdfConverterId>(converterName, ignoreCase: true, out var converter) ||
            !Enum.IsDefined(converter))
        {
            errors.Add($"DocumentConversion route '{family}' references unknown converter '{converterName}'.");
            return null;
        }

        return converter;
    }
}

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
    IReadOnlyList<IDocumentPdfConversionStrategy> GetStrategies(
        DocumentFormat format,
        string extension);
}

internal sealed class DocumentPdfRouteTable : IDocumentPdfRouteTable, IHostedService
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<IDocumentPdfConversionStrategy>> _routes;

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

        var routes = new Dictionary<string, IReadOnlyList<IDocumentPdfConversionStrategy>>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var (extension, converterIds) in DocumentConversionRouting.ParseFormats(options.Value.Formats))
        {
            if (!DocumentFormatDetector.TryGetFormatForExtension(extension, out var format))
            {
                errors.Add($"DocumentConversion format '{extension}' is not registered.");
                continue;
            }

            var route = converterIds
                .Select(converterId => ResolveStrategy(extension, format, converterId, registry, errors))
                .Where(strategy => strategy is not null)
                .ToArray();
            if (!routes.TryAdd(extension, route))
            {
                errors.Add($"DocumentConversion format '{extension}' is duplicated.");
            }
        }

        _routes = routes;
        if (errors.Count > 0)
        {
            throw new OptionsValidationException(
                DocumentConversionOptions.SectionName,
                typeof(DocumentConversionOptions),
                errors);
        }
    }

    public IReadOnlyList<IDocumentPdfConversionStrategy> GetStrategies(
        DocumentFormat format,
        string extension)
    {
        var normalizedExtension = DocumentPdfConversionService.NormalizeExtension(extension);
        if (normalizedExtension is null ||
            !DocumentFormatDetector.TryGetFormatForExtension(normalizedExtension, out var configuredFormat) ||
            configuredFormat != format ||
            !_routes.TryGetValue(normalizedExtension, out var route))
        {
            return [];
        }

        return route;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IDocumentPdfConversionStrategy ResolveStrategy(
        string extension,
        DocumentFormat format,
        DocumentPdfConverterId converterId,
        IReadOnlyDictionary<DocumentPdfConverterId, IDocumentPdfConversionStrategy[]> registry,
        ICollection<string> errors)
    {
        if (!registry.TryGetValue(converterId, out var registrations) || registrations.Length != 1)
        {
            errors.Add($"DocumentConversion format '{extension}' references unregistered converter '{converterId}'.");
            return null;
        }

        var strategy = registrations[0];
        if (!strategy.CanConvert(format))
        {
            errors.Add($"DocumentConversion converter '{converterId}' does not support format '{format}' for extension '{extension}'.");
            return null;
        }

        return strategy;
    }
}

internal static class DocumentConversionRouting
{
    internal const string PassthroughDirective = "passthrough";

    internal static bool IsPassthrough(string value) =>
        string.Equals(value, PassthroughDirective, StringComparison.OrdinalIgnoreCase);

    internal static IReadOnlyDictionary<string, IReadOnlyList<DocumentPdfConverterId>> ParseFormats(
        IReadOnlyDictionary<string, List<string>> configuredFormats)
    {
        var formats = new Dictionary<string, IReadOnlyList<DocumentPdfConverterId>>(
            StringComparer.OrdinalIgnoreCase);
        if (configuredFormats is null)
        {
            return formats;
        }

        foreach (var (configuredExtension, configuredRoute) in configuredFormats)
        {
            var extension = DocumentPdfConversionService.NormalizeExtension(configuredExtension) ??
                configuredExtension;
            formats.TryAdd(
                extension,
                (configuredRoute ?? [])
                    .Where(value => !IsPassthrough(value))
                    .Select(value => Enum.Parse<DocumentPdfConverterId>(value, ignoreCase: true))
                    .ToArray());
        }

        return formats;
    }
}

internal sealed class DocumentConversionOptionsValidator : IValidateOptions<DocumentConversionOptions>
{
    public ValidateOptionsResult Validate(string name, DocumentConversionOptions options)
    {
        var errors = new List<string>();
        ValidateFormats(options.Formats, errors);

        if (options.LibreOfficeProcessTimeoutSeconds >= options.OverallConversionTimeoutSeconds)
        {
            errors.Add("LibreOfficeProcessTimeoutSeconds must be less than OverallConversionTimeoutSeconds.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateFormats(
        IReadOnlyDictionary<string, List<string>> configuredFormats,
        ICollection<string> errors)
    {
        if (configuredFormats is null || configuredFormats.Count == 0)
        {
            errors.Add("DocumentConversion:Formats must not be empty.");
            return;
        }

        var normalizedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (configuredExtension, configuredRoute) in configuredFormats)
        {
            var extension = DocumentPdfConversionService.NormalizeExtension(configuredExtension);
            if (extension is null ||
                !DocumentFormatDetector.TryGetFormatForExtension(extension, out var format))
            {
                errors.Add($"DocumentConversion format '{configuredExtension}' is not registered.");
                continue;
            }

            if (!normalizedExtensions.Add(extension))
            {
                errors.Add($"DocumentConversion format '{configuredExtension}' is duplicated.");
            }

            ValidateRoute(extension, format, configuredRoute, errors);
        }
    }

    private static void ValidateRoute(
        string extension,
        DocumentFormat format,
        IReadOnlyCollection<string> configuredRoute,
        ICollection<string> errors)
    {
        if (configuredRoute is null)
        {
            errors.Add($"DocumentConversion format '{extension}' must provide a converter list.");
            return;
        }

        if (format == DocumentFormat.Pdf)
        {
            if (configuredRoute.Count != 1 ||
                !DocumentConversionRouting.IsPassthrough(configuredRoute.Single()))
            {
                errors.Add(
                    $"DocumentConversion passthrough format '{extension}' must specify only the '{DocumentConversionRouting.PassthroughDirective}' directive.");
            }

            return;
        }

        if (configuredRoute.Count == 0)
        {
            errors.Add($"DocumentConversion format '{extension}' must provide at least one converter.");
            return;
        }

        if (configuredRoute.Any(DocumentConversionRouting.IsPassthrough))
        {
            errors.Add(
                $"DocumentConversion format '{extension}' cannot use the '{DocumentConversionRouting.PassthroughDirective}' directive because it requires conversion to PDF.");
            return;
        }

        var parsed = configuredRoute
            .Select(converterName => ParseConverter(converterName, extension, errors))
            .Where(converter => converter.HasValue)
            .Select(converter => converter.Value)
            .ToArray();
        if (parsed.Length != parsed.Distinct().Count())
        {
            errors.Add($"DocumentConversion format '{extension}' contains duplicate converter identifiers.");
        }
    }

    private static DocumentPdfConverterId? ParseConverter(
        string converterName,
        string extension,
        ICollection<string> errors)
    {
        if (!Enum.TryParse<DocumentPdfConverterId>(converterName, ignoreCase: true, out var converter) ||
            !Enum.IsDefined(converter))
        {
            errors.Add($"DocumentConversion format '{extension}' references unknown converter '{converterName}'.");
            return null;
        }

        return converter;
    }
}

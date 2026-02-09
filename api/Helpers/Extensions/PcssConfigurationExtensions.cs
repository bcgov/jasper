using System;
using System.Collections.Generic;
using System.Linq;
using PCSSCommon.Models;

namespace Scv.Api.Helpers.Extensions;

/// <summary>
/// Extension methods for retrieving typed configuration values from a collection of PcssConfiguration objects
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// Gets an integer configuration value by key, returning a default value if not found or invalid
    /// </summary>
    public static int GetIntValue(this IEnumerable<PcssConfiguration> configData, string key, int defaultValue = 0)
    {
        var config = configData.FirstOrDefault(c => c.Key == key);
        return config != null && int.TryParse(config.Value, out var result)
            ? result
            : defaultValue;
    }

    /// <summary>
    /// Gets a string configuration value by key, returning a default value if not found
    /// </summary>
    public static string GetStringValue(this IEnumerable<PcssConfiguration> configData, string key, string defaultValue = "")
    {
        return configData.FirstOrDefault(c => c.Key == key)?.Value ?? defaultValue;
    }

    /// <summary>
    /// Gets a boolean configuration value by key, returning a default value if not found or invalid
    /// </summary>
    public static bool GetBoolValue(this IEnumerable<PcssConfiguration> configData, string key, bool defaultValue = false)
    {
        var config = configData.FirstOrDefault(c => c.Key == key);
        return config != null && bool.TryParse(config.Value, out var result)
            ? result
            : defaultValue;
    }

    /// <summary>
    /// Gets a double configuration value by key, returning a default value if not found or invalid
    /// </summary>
    public static double GetDoubleValue(this IEnumerable<PcssConfiguration> configData, string key, double defaultValue = 0.0)
    {
        var config = configData.FirstOrDefault(c => c.Key == key);
        return config != null && double.TryParse(config.Value, out var result)
            ? result
            : defaultValue;
    }

    /// <summary>
    /// Gets a delimited configuration value by key and returns it as a list of strings
    /// </summary>
    /// <param name="configData">The configuration data collection</param>
    /// <param name="key">The configuration key to look up</param>
    /// <param name="delimiter">The delimiter used to split the string value (default: ",")</param>
    /// <param name="removeEmptyEntries">Whether to remove empty entries from the result (default: true)</param>
    /// <returns>A list of trimmed string values, or an empty list if not found</returns>
    public static List<string> GetListValue(this IEnumerable<PcssConfiguration> configData, string key, string delimiter = ",", bool removeEmptyEntries = true)
    {
        var config = configData.FirstOrDefault(c => c.Key == key);

        if (config == null || string.IsNullOrWhiteSpace(config.Value))
            return [];

        var splitOptions = removeEmptyEntries
            ? StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            : StringSplitOptions.TrimEntries;

        return [.. config.Value.Split(delimiter, splitOptions)];
    }
}
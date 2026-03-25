using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scv.Api.Infrastructure.ClamAv;

namespace Scv.Api.Infrastructure.HealthChecks
{
    public partial class ClamAvHealthCheck(IClamAvClient clamClient) : IHealthCheck
    {
        private const int MaxDefinitionAgeDays = 7;

        // Clam AV version format: "ClamAV 1.4.2/27521/Mon Mar 24 10:30:00 2026"
        [GeneratedRegex(
            @"^ClamAV\s+(?<clamVersion>\S+)/(?<dbNumber>\d+)/(?<date>\w{3}\s+\w{3}\s+\d{1,2}\s+\d{2}:\d{2}:\d{2}\s+\d{4})",
            RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant)]
        private static partial Regex VersionRegex();

        [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
        private static partial Regex WhitespaceRegex();

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isReachable = await clamClient.PingAsync(cancellationToken);
                if (!isReachable)
                {
                    return HealthCheckResult.Unhealthy("ClamAV daemon is unreachable.");
                }

                var version = await clamClient.GetVersionAsync(cancellationToken);

                if (version == null || VersionRegex().Match(version) is not { Success: true } match)
                {
                    return HealthCheckResult.Degraded($"Unable to parse ClamAV version string: '{version}'.");
                }

                var dbNumber = match.Groups["clamVersion"].Value;
                var dateStr = WhitespaceRegex().Replace(match.Groups["date"].Value, " ");

                var data = new Dictionary<string, object>
                {
                    ["version"] = version ?? string.Empty,
                    ["dbNumber"] = dbNumber,
                    ["rawDate"] = dateStr,
                };

                if (!DateTimeOffset.TryParseExact(dateStr, "ddd MMM d HH:mm:ss yyyy",
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dbDate))
                {
                    return HealthCheckResult.Degraded(
                        $"Unable to parse definition date: '{dateStr}'. Raw version: '{version}'.", data: data);
                }

                data["dbDate"] = dbDate.ToString("yyyy-MM-dd");

                var ageInDays = (DateTimeOffset.UtcNow - dbDate).TotalDays;
                data["ageInDays"] = (int)ageInDays;

                if (ageInDays > MaxDefinitionAgeDays)
                {
                    return HealthCheckResult.Degraded(
                        $"ClamAV definitions are {(int)ageInDays} day(s) old (last updated: {dbDate:yyyy-MM-dd}). " +
                        $"Max allowed: {MaxDefinitionAgeDays} days. Verify freshclamd is running.",
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    $"ClamAV OK — DB version: {dbNumber}, updated: {dbDate:yyyy-MM-dd}.", data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("ClamAV health check threw an exception.", ex);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PCSSCommonConstants = PCSSCommon.Common.Constants;

namespace Scv.Api.Helpers.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T> OrderByDateString<T>(
        this IEnumerable<T> source,
        Func<T, string> dateSelector,
        bool descending = false,
        bool nullsLast = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(dateSelector);

        var ordered = source.Select(item => new
        {
            Item = item,
            DateValue = ParseDateString(dateSelector(item), nullsLast)
        });

        return descending
            ? ordered.OrderByDescending(x => x.DateValue).Select(x => x.Item)
            : ordered.OrderBy(x => x.DateValue).Select(x => x.Item);
    }

    private static DateTime ParseDateString(string dateString, bool nullsLast)
    {
        var defaultValue = nullsLast ? DateTime.MaxValue : DateTime.MinValue;

        if (string.IsNullOrWhiteSpace(dateString))
        {
            return defaultValue;
        }

        // Try the usual date format first (dd-MMM-yyyy, e.g., "24-Oct-2025")
        if (DateTime.TryParseExact(
            dateString,
            PCSSCommonConstants.DATE_FORMAT,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime date))
        {
            return date;
        }

        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fallbackDate))
        {
            return fallbackDate;
        }

        return defaultValue;
    }
}

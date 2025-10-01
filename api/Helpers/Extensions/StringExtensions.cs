using System;

namespace Scv.Api.Helpers.Extensions
{
    public static class StringExtensions
    {
        public static string EnsureEndingForwardSlash(this string target) => target.EndsWith("/") ? target : $"{target}/";
        public static string ReturnNullIfEmpty(this string target) => string.IsNullOrEmpty(target) ? null : target;

        public static string ConvertNameLastCommaFirstToFirstLast(this string name)
        {
            var names = name?.Split(",");
            return names?.Length == 2 ? $"{names[1].Trim()} {names[0].Trim()}" : name;
        }

        public static (string lastName, string firstName) SplitFullNameToFirstAndLast(this string fullName, string delimiter = ",")
        {
            string lastName = fullName ?? string.Empty;
            string firstName = string.Empty;

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                var parts = fullName.Split(delimiter, StringSplitOptions.TrimEntries);

                if (parts.Length > 0)
                {
                    lastName = parts[0];

                    if (parts.Length > 1)
                    {
                        firstName = parts.Length == 2
                            ? parts[1]
                            : string.Join(delimiter, parts, 1, parts.Length - 1);
                    }
                }
            }

            return (lastName, firstName);
        }

    }
}

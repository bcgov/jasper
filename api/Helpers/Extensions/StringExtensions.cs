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
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return (fullName, "");
            }

            var parts = fullName.Split(delimiter, StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                return (fullName, "");
            }

            if (parts.Length == 1)
            {
                return (parts[0], "");
            }

            if (parts.Length == 2)
            {
                return (parts[0], parts[1]);
            }

            var combinedFirst = string.Join(delimiter, parts, 1, parts.Length - 1);
            return (parts[0], combinedFirst);
        }
    }
}

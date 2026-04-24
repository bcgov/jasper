using System.IO.Compression;
using System.Text;

namespace Scv.Core.Helpers.Extensions
{
    public static class StringExtensions
    {
        public static string EnsureEndingForwardSlash(this string target) => target.EndsWith('/') ? target : $"{target}/";
        public static string? ReturnNullIfEmpty(this string target) => string.IsNullOrEmpty(target) ? null : target;

        public static int GetUtf8Size(this string value) => value == null ? 0 : Encoding.UTF8.GetByteCount(value);

        public static string CompressToBase64(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var inputBytes = Encoding.UTF8.GetBytes(value);
            using var outputStream = new MemoryStream();
            using (var gzip = new GZipStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzip.Write(inputBytes, 0, inputBytes.Length);
            }

            return Convert.ToBase64String(outputStream.ToArray());
        }

        public static string DecompressFromBase64(this string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
            {
                return string.Empty;
            }

            var compressedBytes = Convert.FromBase64String(base64);
            using var inputStream = new MemoryStream(compressedBytes);
            using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();
            gzip.CopyTo(outputStream);

            return Encoding.UTF8.GetString(outputStream.ToArray());
        }

        public static string? ConvertNameLastCommaFirstToFirstLast(this string? name)
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

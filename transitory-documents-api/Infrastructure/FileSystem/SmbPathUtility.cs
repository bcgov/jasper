namespace Scv.TdApi.Infrastructure.FileSystem
{
    using Scv.Core.Helpers.Exceptions;
    using System.IO;

    /// <summary>
    /// Utility class for SMB path normalization and manipulation.
    /// Centralizes path handling logic to ensure consistency across the application.
    /// </summary>
    public static class SmbPathUtility
    {
        /// <summary>
        /// Returns the directory portion of an SMB path using SMB separators.
        /// This is OS-agnostic and does not rely on the host path separator.
        /// </summary>
        public static string GetDirectoryPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var normalized = path.Replace('/', '\\').TrimEnd('\\');
            if (string.IsNullOrEmpty(normalized))
            {
                return string.Empty;
            }

            var lastSeparatorIndex = normalized.LastIndexOf('\\');
            if (lastSeparatorIndex < 0)
            {
                return string.Empty;
            }

            return normalized[..lastSeparatorIndex];
        }

        /// <summary>
        /// Returns the file name portion of an SMB path using SMB separators.
        /// This is OS-agnostic and does not rely on the host path separator.
        /// </summary>
        public static string GetFileName(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var normalized = path.Replace('/', '\\').TrimEnd('\\');
            if (string.IsNullOrEmpty(normalized))
            {
                return string.Empty;
            }

            var lastSeparatorIndex = normalized.LastIndexOf('\\');
            if (lastSeparatorIndex < 0)
            {
                return normalized;
            }

            return normalized[(lastSeparatorIndex + 1)..];
        }

        /// <summary>
        /// Validates that a path is relative and does not contain parent directory traversal.
        /// </summary>
        public static void ValidateRelativePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var normalized = relativePath.Replace("/", "\\");

            if (normalized.StartsWith("\\\\", StringComparison.Ordinal) || normalized.StartsWith("\\", StringComparison.Ordinal))
            {
                throw new BadRequestException("path must be a relative path.");
            }

            var trimmed = normalized.TrimStart('\\');

            if (trimmed.Length >= 2 && char.IsLetter(trimmed[0]) && trimmed[1] == ':')
            {
                throw new BadRequestException("path must be a relative path.");
            }

            if (Path.IsPathRooted(normalized))
            {
                throw new BadRequestException("path must be a relative path.");
            }

            var segments = trimmed.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => segment == ".."))
            {
                throw new BadRequestException("path must not contain '..' segments.");
            }
        }

        /// <summary>
        /// Normalizes a path by converting slashes and removing share prefixes.
        /// Example input: "/Criminal Share/Fraser+Vancouver Coastal/222 main/2025/10 October/October 1 (Wed)/101/File.pdf"
        /// Example output: "Fraser+Vancouver Coastal\222 main\2025\10 October\October 1 (Wed)\101\File.pdf"
        /// </summary>
        /// <param name="path">The path to normalize</param>
        /// <returns>Normalized path with backslashes and no share prefix</returns>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            var normalized = path.Replace('/', '\\').Trim('\\');

            var segments = normalized.Split(['\\'], StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
                return path;

            return string.Join("\\", segments);
        }

        /// <summary>
        /// Converts a relative path to an SMB path by combining with base path and normalizing.
        /// Use this for paths that need the base path prepended.
        /// </summary>
        /// <param name="basePath">The base path (optional)</param>
        /// <param name="relativePath">The relative path</param>
        /// <returns>Normalized SMB path with backslashes</returns>
        public static string GetSmbPath(string? basePath, string? relativePath)
        {
            var trimmedBasePath = basePath?.Trim('/', '\\') ?? string.Empty;
            var trimmedRelativePath = relativePath?.Trim('/', '\\') ?? string.Empty;

            var parts = new[] { trimmedBasePath, trimmedRelativePath }
                .Where(part => !string.IsNullOrEmpty(part));

            return string.Join("\\", parts).Replace('/', '\\');
        }

        /// <summary>
        /// Combines two SMB path segments with proper separator handling.
        /// </summary>
        /// <param name="path1">First path segment</param>
        /// <param name="path2">Second path segment</param>
        /// <returns>Combined SMB path</returns>
        public static string CombinePath(string path1, string path2)
        {
            return $"{path1.TrimEnd('\\')}\\{path2.TrimStart('\\')}";
        }

        /// <summary>
        /// Combines a path with forward slashes (used for folder construction).
        /// </summary>
        /// <param name="parts">Path parts to combine</param>
        /// <returns>Combined path with forward slashes</returns>
        public static string CombinePathWithForwardSlashes(params string[] parts)
        {
            return string.Join("/", parts.Select(p => p.Trim('/', '\\')));
        }
    }
}

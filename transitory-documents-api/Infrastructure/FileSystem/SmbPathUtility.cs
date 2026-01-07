namespace Scv.TdApi.Infrastructure.FileSystem
{
    /// <summary>
    /// Utility class for SMB path normalization and manipulation.
    /// Centralizes path handling logic to ensure consistency across the application.
    /// </summary>
    public static class SmbPathUtility
    {
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

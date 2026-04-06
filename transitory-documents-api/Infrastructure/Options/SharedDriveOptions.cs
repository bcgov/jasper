using System.ComponentModel.DataAnnotations;

namespace Scv.TdApi.Infrastructure.Options
{
    public sealed class SharedDriveOptions
    {
        /// <summary>This is the base path under the share- essentially any path in the share before the region. 
        /// ex: \\SmbServer\SmbShareName\BASE\PATH\region\location\date\room</summary>
        [Required, MinLength(1)]
        public string BasePath { get; set; } = default!;

        /// <summary>One or more folder-name formats for the day folder under {location}.</summary>
        [Required]
        public List<string> DateFolderFormats { get; set; } = new() { };

        /// <summary>The name of the Smb Server. Ex: alexandria.provjud.local.</summary>
        public string? SmbServer { get; set; }

        /// <summary>The name of the share on the SmbServer. Ex: province_dev$</summary>
        public string? SmbShareName { get; set; }

        /// <summary>SMB username, prepended with SmbDomain\.</summary>
        public string? SmbUsername { get; set; }

        /// <summary>SMB drive password.</summary>
        public string? SmbPassword { get; set; }

        /// <summary>SMB username login domain, generally PROVJUD.</summary>
        public string? SmbDomain { get; set; }

        /// <summary>
        /// Maximum number of retries for transient SMB errors.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Initial retry delay (ms) for exponential backoff.
        /// </summary>
        public int InitialRetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// The buffer size (in bytes) to use when reading files from the SMB share.
        /// </summary>
        public int FileBufferSize { get; set; } = 65536; // 64 KB

        /// <summary>
        /// Maximum concurrency when traversing a room subtree (parallel directory enumeration).
        /// Recommended default: 4.
        /// </summary>
        public int DirectoryListingMaxConcurrency { get; set; } = 4;
    }
}
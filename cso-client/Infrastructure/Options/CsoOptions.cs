using System.ComponentModel.DataAnnotations;

namespace Scv.Cso.Infrastructure.Options;

public sealed class CsoOptions
{
    [Required]
    public string BaseUrl { get; set; } = default!;

    [Required]
    public string ActionUri { get; set; } = default!;
}

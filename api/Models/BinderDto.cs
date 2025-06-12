using System.Collections.Generic;

namespace Scv.Api.Models;

public class BinderDto : BaseDto
{
    public Dictionary<string, string> Labels { get; set; } = [];
    public List<TagDto> Tags { get; set; } = [];
    public List<BinderDocumentDto> Documents { get; set; } = [];
}

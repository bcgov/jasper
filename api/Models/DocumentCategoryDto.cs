namespace Scv.Api.Models;

public class DocumentCategoryDto : BaseDto
{
    public string Name { get; set; }
    public string Value { get; set; }
    public int ExternalId { get; set; }
}

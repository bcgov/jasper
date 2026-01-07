namespace Scv.Models
{
    public class BinderDto : BaseDto
    {
        public Dictionary<string, string> Labels { get; set; } = [];
        public List<TagDto> Tags { get; set; } = [];
        public List<BinderDocumentDto> Documents { get; set; } = [];
        public DateTime? UpdatedDate { get; set; }
    }
}

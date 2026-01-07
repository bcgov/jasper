namespace Scv.Models
{
    public class QuickLinkDto : BaseDto
    {
        public string Name { get; set; }
        public string ParentName { get; set; }
        public bool IsMenu { get; set; }
        public string URL { get; set; }
        public int Order { get; set; }
        public string JudgeId { get; set; }
    }
}

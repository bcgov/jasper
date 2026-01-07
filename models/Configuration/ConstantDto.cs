namespace Scv.Models.Configuration
{

    public class ConstantDto : AuditableDto
    {
        public string Key { get; set; } = string.Empty;
        public List<string> Values { get; set; } = [];
    }
}
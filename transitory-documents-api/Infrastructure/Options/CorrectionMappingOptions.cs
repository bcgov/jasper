namespace Scv.TdApi.Infrastructure.Options
{
    public class CorrectionMappingOptions
    {
        public List<CorrectionMapping> VirtualBailMappings { get; set; } = [];
        public List<CorrectionMapping> RegionMappings { get; set; } = [];
        public List<CorrectionMapping> LocationMappings { get; set; } = [];
    }

    public class CorrectionMapping
    {
        public string? Target { get; set; }
        public string? Replacement { get; set; }
        public bool? IgnoreRoom { get; set; }
    }
}

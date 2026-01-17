namespace Scv.TdApi.Infrastructure.Options
{
    public class CorrectionMappingOptions
    {
        public List<CorrectionMapping> VirtualBailMappings { get; set; } = new() { };
        public List<CorrectionMapping> RegionMappings { get; set; } = new() { };
        public List<CorrectionMapping> LocationMappings { get; set; } = new() { };
    }

    public class CorrectionMapping
    {
        public string? Target { get; set; }
        public string? Replacement { get; set; }
        public bool? IgnoreRoom { get; set; }
    }
}
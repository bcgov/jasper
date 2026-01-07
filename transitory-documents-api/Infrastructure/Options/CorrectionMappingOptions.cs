namespace Scv.TdApi.Infrastructure.Options
{
    public sealed class CorrectionMappingOptions
    {
        public List<CorrectionMapping> RegionMappings { get; set; } = new() { };
        public List<CorrectionMapping> LocationMappings { get; set; } = new() { };
    }

    public sealed class CorrectionMapping
    {
        public string Target { get; set; } = default!;
        public string Replacement { get; set; } = default!;
    }
}
namespace Scv.Models.Region
{
    public class Region
    {
        public required string Name { get; set; }
        public required string Code { get; set; }
        public bool? Active { get; set; } = true;
        public ICollection<WorkArea.WorkArea> WorkAreas { get; set; } = new List<WorkArea.WorkArea>();
    }
}
namespace Scv.Models.Calendar
{
    public class Presider
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Initials { get; set; }
        public int HomeLocationId { get; set; }
        public string HomeLocationName { get; set; }
    }
}

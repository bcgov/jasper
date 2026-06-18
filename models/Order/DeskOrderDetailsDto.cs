namespace Scv.Models.Order
{
    public class DeskOrderDetailsDto
    {
        public string Directions { get; set; }
        public OrderTermDto[] OrderTerms { get; set; } = [];
    }
}

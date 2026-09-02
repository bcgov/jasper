namespace Scv.Models.Order
{
    public class DeskOrderDetailsDto
    {
        public string ReasonsForRejection { get; set; }
        public string Directions { get; set; }
        public OrderTermDto[] OrderTerms { get; set; } = [];
        public bool IsClerkToSign { get; set; }
    }
}

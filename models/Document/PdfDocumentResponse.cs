namespace Scv.Models.Document
{
    public class PdfDocumentResponse
    {
        public string Base64Pdf { get; set; }

        public List<PageRange> PageRanges { get; set; } = [];
    }

    public class PageRange
    {
        public int Start { get; set; }
        public int End { get; set; }
    }
}
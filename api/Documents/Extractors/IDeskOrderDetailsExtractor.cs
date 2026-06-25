using System.IO;
using Scv.Models.Order;

namespace Scv.Api.Documents.Extractors
{
    public interface IDeskOrderDetailsExtractor
    {
        DeskOrderDetailsDto Extract(Stream stream);
    }
}

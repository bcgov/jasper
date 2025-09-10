using System.Collections.Generic;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using Scv.Api.Models.Criminal.Detail;

namespace Scv.Api.Documents;

public interface IDocumentConverter
{
    Task<ICollection<CriminalDocument>> GetCriminalDocuments(CfcAccusedFile ac);
}

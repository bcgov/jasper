
namespace Scv.TdApi.Models
{
    public class DateFolderCandidate(string fullPath, string dateFolderName)
    {
        public string FullPath { get; } = fullPath;
        public string DateFolderName { get; } = dateFolderName;
    }
}

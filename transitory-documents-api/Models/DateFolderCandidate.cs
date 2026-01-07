
namespace Scv.TdApi.Models
{
    public class DateFolderCandidate
    {
        public string FullPath { get; }
        public string DateFolderName { get; }

        public DateFolderCandidate(string fullPath, string dateFolderName)
        {
            FullPath = fullPath;
            DateFolderName = dateFolderName;
        }
    }
}
using System.ComponentModel.DataAnnotations;

namespace Scv.Db.Models
{
    public class MigrationHistory
    {
        [Key]
        public string MigrationId { get; set; }
        public string ProductVersion { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Scv.Db.Models
{
    public class SignalROutboxMessage
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public string Channel { get; set; }
        public string UserId { get; set; }
        [Required]
        public string EnvelopeJson { get; set; }
        public bool AckRequired { get; set; }
        public Guid? AckGuid { get; set; }
        public DateTimeOffset? AckedAt { get; set; }
        public string AckedBy { get; set; }
        public int OfflineMinutes { get; set; } = 1; // The number of minutes to allow polling behavior for unsent messages when the user is not connected.
        [Required]
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? DeliveredAt { get; set; }
        public string DeliveredBy { get; set; }
    }
}

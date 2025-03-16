using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace userstrctureapi.Models
{
    [Table("audit_logs")]
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string EntityName { get; set; } = string.Empty;

        [Required]
        public string Action { get; set; } = string.Empty; // Added, Modified, Deleted

        public string? Changes { get; set; } // เก็บ JSON หรือข้อความแสดงการเปลี่ยนแปลง

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? UserId { get; set; } // ใครเป็นคนแก้ไข?
    }
}
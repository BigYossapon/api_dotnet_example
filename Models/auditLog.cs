using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace userstrctureapi.Models
{
    [Table("audit_logs")]
    public class AuditLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("entity_name")]
        public string EntityName { get; set; } = string.Empty;
        [Column("action")]
        public string Action { get; set; } = string.Empty;// Insert, Update, Delete
        [Column("changes", TypeName = "jsonb")]
        public JsonDocument Changes { get; set; } // JSON ของค่าที่เปลี่ยนแปลง
        [Column("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;// ผู้ที่กระทำการ
    }
}
using System.ComponentModel.DataAnnotations;

namespace userstrctureapi.Models
{
    public class User
    {
        [Key]
        public int user_id { get; set; }
        public string phone { get; set; } = string.Empty;
        public string? otp { get; set; }
        public DateTime? OtpExpiry { get; set; }

        public List<RefreshToken> RefreshTokens { get; set; } = new();
    }
}
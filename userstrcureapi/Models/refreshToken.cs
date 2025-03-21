using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace userstrctureapi.Models
{

    public class RefreshToken
    {

        [Key]
        public int id { get; set; }

        [ForeignKey("User")]
        public int user_id { get; set; }
        public string token { get; set; } = string.Empty;
        public DateTime expiry { get; set; }

        public User User { get; set; } = null!;
    }
}
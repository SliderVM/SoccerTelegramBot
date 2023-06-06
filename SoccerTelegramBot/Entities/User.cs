using System.ComponentModel.DataAnnotations;

namespace SoccerTelegramBot.Entities
{
    public class User
    {
        [Key]
        public long Id { get; set; }
        [Required]
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public bool IsAdmin { get; set; }        
        public List<Subscription>? Subscriptions { get; set;}
        public List<Signed>? Signeds { get; set; }
    }
}

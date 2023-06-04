using System.ComponentModel.DataAnnotations;

namespace SoccerTelegramBot.Entities
{
    public class Subscription
    {
        [Key]
        public int Id { get; set; }        
        public User? User { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }        
        public bool IsActive { get; set; }
    }
}

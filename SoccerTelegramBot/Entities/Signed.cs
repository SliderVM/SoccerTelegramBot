using System.ComponentModel.DataAnnotations;

namespace SoccerTelegramBot.Entities
{
    public class Signed
    {
        [Key] 
        public int Id { get; set; }
        public DateTime GameDate { get; set; }        
        public User? User { get; set; }
        public bool IsPayment { get; set; }
    }
}

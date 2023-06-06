using System.ComponentModel.DataAnnotations;

namespace SoccerTelegramBot.Entities
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public string? NotificationLabel { get; set; }
        public string? Value { get; set; }
    }
}

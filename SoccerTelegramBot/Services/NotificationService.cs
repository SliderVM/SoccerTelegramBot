using Microsoft.EntityFrameworkCore;
using SoccerTelegramBot.Entities;

namespace SoccerTelegramBot.Services
{
    public class NotificationService
    {
        private readonly DatabaseContext _databaseContext;

        private const string NOTIFICATION_LABEL = "nextnotificationdate";

        public NotificationService(DatabaseContext db)
        {
            _databaseContext = db;
        }

        public async Task<DateTime> GetNotificationDate()
        {
            Notification notification = await _databaseContext.Notifications.FirstOrDefaultAsync(x => x.NotificationLabel.Equals(NOTIFICATION_LABEL));

            return DateTime.Parse(notification.Value).Date;
        }

        public async Task<Notification> SetNotificationAsync(DateOnly dateOnly)
        {
            Notification notification = await _databaseContext.Notifications.FirstOrDefaultAsync(x => x.NotificationLabel.Equals(NOTIFICATION_LABEL));

            if (notification == null)
            {
                notification = new Notification()
                {
                    NotificationLabel = NOTIFICATION_LABEL,
                    Value = dateOnly.ToString()
                };

                _databaseContext.Notifications.Add(notification);
            }
            else
            {
                notification.Value = dateOnly.ToString();
                _databaseContext.Notifications.Update(notification);
            }

            await _databaseContext.SaveChangesAsync();

            return notification;
        }
    }
}

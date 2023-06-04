using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoccerTelegramBot.Abstract;

namespace SoccerTelegramBot.Services
{
    public class PollingService : PollingServiceBase<ReceiverService>
    {
        public PollingService(IServiceProvider serviceProvider, ILogger<PollingService> logger, DatabaseContext database)
            : base(serviceProvider, logger)
        {
            database.Database.Migrate();
        }
    }
}
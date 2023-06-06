using Microsoft.Extensions.Logging;
using SoccerTelegramBot.Abstract;
using Telegram.Bot;

namespace SoccerTelegramBot.Services
{
    public class ReceiverService : ReceiverServiceBase<UpdateHandler>
    {
        public ReceiverService(
        ITelegramBotClient botClient,
        UpdateHandler updateHandler,
        ILogger<ReceiverServiceBase<UpdateHandler>> logger)
        : base(botClient, updateHandler, logger)
        {
        }
    }
}

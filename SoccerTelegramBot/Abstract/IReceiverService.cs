namespace SoccerTelegramBot.Abstract
{
    public interface IReceiverService
    {
        Task ReceiveAsync(CancellationToken stoppingToken);
    }
}

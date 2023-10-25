using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace SoccerTelegramBot.Services
{
    public class SendNotificationService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly GameDay _gameDay;
        private readonly NotificationService _notificationService;
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly UserService _userService;
        private readonly DatabaseContext _databaseContext;

        private Task? _executingTask;
        private CancellationTokenSource? _stoppingCts;        

        public SendNotificationService(ILogger<SendNotificationService> logger, GameDay gameDay, NotificationService notificationService, ITelegramBotClient botClient, UserService userService, DatabaseContext databaseContext)
        {
            _logger = logger;
            _gameDay = gameDay;
            _notificationService = notificationService;
            _telegramBotClient = botClient;
            _userService = userService;
            _databaseContext = databaseContext;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Notification service");
            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executingTask = DoWork(cancellationToken);

            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts!.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
            }
        }

        private async Task DoWork(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var currentDay = DateTime.Now;
                    var dayNotification = await _notificationService.GetNotificationDate();
                    dayNotification = dayNotification.AddHours(12);
                    if (currentDay > dayNotification)
                    {
                        var subscriptions = await _databaseContext.Subscriptions.Where(x => x.IsActive && x.Year.Equals(currentDay.Year) && x.Month.Equals(currentDay.Month)).Include(x => x.User).ToArrayAsync(cancellationToken);
                        var gameDate = await _gameDay.GetDateGameAsync();
                        var signinUsers = await _databaseContext.Signeds.Where(x => x.GameDate.Equals(gameDate)).Include(x => x.User).ToArrayAsync(cancellationToken);
                        var userForGroupNotification = String.Empty;

                        var text = "Не забудь записаться на завтрашнюю инру! Нажми /signup";
                        foreach (var subscription in subscriptions)
                        {
                            var sendUserId = subscription?.User?.Id ?? 0;
                            var siginUser = signinUsers.FirstOrDefault(x => x.User.Id.Equals(sendUserId));

                            if (sendUserId == 0 || siginUser is not null)
                            {
                                continue;
                            }

                            userForGroupNotification += $"@{subscription.User.UserName} ";

                            try
                            {
                                await _telegramBotClient.SendTextMessageAsync(
                                                chatId: sendUserId,
                                                text: text,
                                                replyMarkup: new ReplyKeyboardRemove(),
                                                cancellationToken: cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex.Message);
                            }
                        }

                        userForGroupNotification += text;
                        
                        // TODO Создать отдельный GroupService
                        var group = await _databaseContext.Configurations.Where(x => x.Label.Equals("groupid")).FirstOrDefaultAsync(cancellationToken);
                        if (group is not null)
                        {
                            _ = await _telegramBotClient.SendTextMessageAsync(
                                    chatId: group?.Value,
                                    text: userForGroupNotification,
                                    replyMarkup: new ReplyKeyboardRemove(),
                                    cancellationToken: cancellationToken);
                        }

                        await _notificationService.SetNotificationAsync(DateOnly.FromDateTime(dayNotification.AddDays(7)));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }

                await Task.Delay(60000, cancellationToken);
            }
        }

        public void Dispose()
        {
            _stoppingCts?.Cancel();
        }
    }
}

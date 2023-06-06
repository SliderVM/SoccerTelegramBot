using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoccerTelegramBot.Data;
using SoccerTelegramBot.Entities;
using System.Data;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SoccerTelegramBot.Services
{
    public class UpdateHandler : IUpdateHandler
    {

        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<UpdateHandler> _logger;
        private readonly DatabaseContext _databaseContext;
        private readonly GameDay _gameDay;
        private readonly TimeOnly _timeLimitForSubsctibe = new(14, 00);
        private readonly UserService _userService;
        private readonly StepService _stepService;
        private readonly RulesService _rulesService;
        private readonly NotificationService _notificationService;

        const string _commands = "/help - список команд\r\n" +
                "/signup - записаться на игру\r\n" +
                "/refuse - отказаться от записи на игру\r\n" +
                "/freesubscriptions - количество свободных абонементов\r\n" +
                "/nspayment - уведомление об оплате абонимента\r\n" +
                "/nogpayment - уведомление об оплате 1 игры\r\n" +
                "/listsubscription - список купивших абонемент\r\n" +
                "/rules - правила\r\n" +
                "/costonegame - стоимость одной игры\r\n" +
                "/costsubscribe - стоимость абонемента\r\n" +
                "/gameday - день игры (ближайшая игра)\r\n" +
                "/getmyid - получить свой telegramId\r\n";
        const string GROUPID = "groupid";
        const int MAX_SUBSCRIBSION = 15;


        public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger, DatabaseContext databaseContext, GameDay gameDay, UserService userService, StepService stepService, RulesService rulesService, NotificationService notificationService)
        {
            _botClient = botClient;
            _logger = logger;
            _databaseContext = databaseContext;
            _gameDay = gameDay;
            _userService = userService;
            _stepService = stepService;
            _rulesService = rulesService;
            _notificationService = notificationService;
        }

        public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

            // Cooldown in case of network connection error
            if (exception is RequestException)
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update switch
            {
                { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
                _ => UnknownUpdateHandlerAsync(update)
            };

            await handler;
        }

        private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Receive message type: {MessageType}", message.Type);
            if (message.Text is not { } messageText)
                return;

            if (message.Chat is { } messageChat)
            {
                if (messageChat.Type.Equals(ChatType.Supergroup))
                {
                    var groupId = await _databaseContext.Configurations.Where(x => x.Label.Equals(GROUPID)).FirstOrDefaultAsync(cancellationToken);
                    if (groupId is null)
                    {
                        var confItem = new Configuration()
                        {
                            Label = GROUPID,
                            Name = "Группа в которую пересылать сообщения бота",
                            Value = message.Chat.Id.ToString(),
                        };

                        _databaseContext.Add(confItem);
                        await _databaseContext.SaveChangesAsync();
                        groupId = confItem;
                    }
                }
            }

            var action = messageText.Split(' ')[0] switch
            {
                "/help" => Help(message, cancellationToken),
                "/help@SoccerTelegramBot" => HelpFromGroup(message, cancellationToken),
                "/signup" => SignUpGroup(_botClient, message, cancellationToken, _databaseContext, _gameDay, _timeLimitForSubsctibe, _userService),
                "/signup@SoccerTelegramBot" => SignUpGroup(_botClient, message, cancellationToken, _databaseContext, _gameDay, _timeLimitForSubsctibe, _userService),
                "/refuse" => Refuse(_botClient, message, cancellationToken, _databaseContext, _userService, _gameDay, _timeLimitForSubsctibe),
                "/refuse@SoccerTelegramBot" => Refuse(_botClient, message, cancellationToken, _databaseContext, _userService, _gameDay, _timeLimitForSubsctibe),
                "/freesubscriptions" => FreeSubscriptions(message, cancellationToken),
                "/freesubscriptions@SoccerTelegramBot" => FreeSubscriptions(message, cancellationToken),
                "/nspayment" => NSPayment(message, cancellationToken),
                "/nspayment@SoccerTelegramBot" => NSPayment(message, cancellationToken),
                "/listsubscription" => ListSubscription(message, cancellationToken),
                "/listsubscription@SoccerTelegramBot" => ListSubscription(message, cancellationToken),
                "/rules" => GetRules(message, cancellationToken),
                "/rules@SoccerTelegramBot" => GetRules(message, cancellationToken),
                "/setgameday" => SetGameday(message, cancellationToken),
                "/setgameday@SoccerTelegramBot" => SetGameday(message, cancellationToken),
                "/addsubscriptions" => AddSubscription(message, cancellationToken),
                "/addsubscriptions@SoccerTelegramBot" => AddSubscription(message, cancellationToken),
                "/setrules" => SetRules(message, cancellationToken),
                "/setrules@SoccerTelegramBot" => SetRules(message, cancellationToken),
                "/costonegame" => Usage(_botClient, message, cancellationToken),
                "/costonegame@SoccerTelegramBot" => Usage(_botClient, message, cancellationToken),
                "/costsubscribe" => Usage(_botClient, message, cancellationToken),
                "/costsubscribe@SoccerTelegramBot" => Usage(_botClient, message, cancellationToken),
                "/gameday" => GameDay(_botClient, message, cancellationToken, _databaseContext, _gameDay),
                "/gameday@SoccerTelegramBot" => GameDay(_botClient, message, cancellationToken, _databaseContext, _gameDay),
                "/getmyid" => GetMyId(_botClient, message, cancellationToken),
                "/getmyid@SoccerTelegramBot" => GetMyId(_botClient, message, cancellationToken),
                _ => Usage(_botClient, message, cancellationToken)
            };

            Message sentMessage = await action;
            _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

            static async Task<Message> SignUpGroup(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, DatabaseContext db, GameDay gameDay, TimeOnly timeLimitForSubsctibe, UserService userService)
            {
                var user = await userService.GetUserAsync(message?.From, cancellationToken);

                var gameDate = await gameDay.GetDateGameAsync();
                var gameTime = await gameDay.GetGameTimeAsync();

                var gameMonth = gameDate.Month;
                var gameYear = gameDate.Year;

                var signed = user.Signeds?.FirstOrDefault(x => x.GameDate.Date.Equals(gameDate.Date));
                var subscription = user.Subscriptions?.LastOrDefault();

                var isPayment = false;
                if (subscription is not null)
                {
                    if (gameMonth == subscription.Month && gameYear == subscription.Year && subscription.IsActive)
                    {
                        isPayment = true;
                    }
                }

                var text = string.Empty;

                if (!isPayment || (DateTime.Now.Date < gameDate && DateTime.Now.ToShortTimeString().Equals(timeLimitForSubsctibe.ToShortTimeString())))
                {
                    text += $"У вас нет подписки, запись на игру откроется после {gameDate.Date} {timeLimitForSubsctibe.ToShortTimeString()}\n\n";

                    text += await GetGameSignedInList(gameDate, db, cancellationToken);

                    return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: text,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    if (signed == null)
                    {
                        var countSign = db.Signeds.Where(x => x.GameDate.Date.Equals(gameDate.Date)).Count();
                        if (countSign > MAX_SUBSCRIBSION)
                        {
                            text += "Кто не успел - тот опаздал, больще 15 человек нельзя!\n";
                        }
                        else
                        {
                            var newSigned = new Signed()
                            {
                                GameDate = gameDate,
                                User = user,
                                IsPayment = isPayment,
                            };

                            db.Add(newSigned);
                            await db.SaveChangesAsync(cancellationToken);
                        }
                    }

                    string dateTime = await gameDay.GetDayAsync();
                    text = dateTime + "\n";

                    text += await GetGameSignedInList(gameDate, db, cancellationToken);

                    SendMessageToGroup(botClient, message, text, cancellationToken, db);

                    return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: text,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
                }
            }

            static async Task<Message> GameDay(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, DatabaseContext db, GameDay gameDay)
            {
                var dateTime = await gameDay.GetDayAsync();

                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: dateTime,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            static async Task<Message> Refuse(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, DatabaseContext db, UserService userService, GameDay gameDay, TimeOnly timeLimitForSubsctibe)
            {
                var user = userService.GetUserAsync(message?.From, cancellationToken);
                var gameDate = await gameDay.GetDateGameAsync();

                var signIn = await db.Signeds.Where(x => x.GameDate.Date.Equals(gameDate.Date)).FirstOrDefaultAsync(cancellationToken);

                if (signIn is not null)
                {
                    db.Signeds.Remove(signIn);
                }

                var text = await GetGameSignedInList(gameDate, db, cancellationToken);

                SendMessageToGroup(botClient, message, text, cancellationToken, db);

                return await botClient.SendTextMessageAsync(
                        chatId: message?.Chat?.Id,
                        text: text,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
            }

            static async Task<Message> GetMyId(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
            {
                string userId = message?.From?.Id.ToString() ?? "";

                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: userId,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }

            static async Task<string> GetGameSignedInList(DateTime gameDate, DatabaseContext db, CancellationToken cancellationToken)
            {
                var text = String.Empty;
                var i = 1;
                await db.Signeds.Where(x => x.GameDate.Date.Equals(gameDate.Date)).ForEachAsync(x =>
                {
                    text += $"{i}. {x.User?.FirstName} {x.User?.LastName} {x.User?.UserName} {x.User?.Id}\n";
                    i++;
                }, cancellationToken);

                return text;
            }

            /// TODO избавиться от всех статических методов, этот метод перенсен в статический
            static async void SendMessageToGroup(ITelegramBotClient botClient, Message? message, string text, CancellationToken cancellationToken,
                DatabaseContext db)
            {
                if (message?.Chat?.Id == message?.From?.Id)
                {
                    var group = await db.Configurations.Where(x => x.Label.Equals(GROUPID)).FirstOrDefaultAsync(cancellationToken);
                    if (group is not null)
                    {
                        _ = await botClient.SendTextMessageAsync(
                                chatId: group.Value,
                                text: text,
                                replyMarkup: new ReplyKeyboardRemove(),
                                cancellationToken: cancellationToken);
                    }
                }
            }
        }

        private async Task<Message> HelpFromGroup(Message message, CancellationToken cancellationToken)
        {
            return await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: _commands,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
        }

        private async Task<Message> Help(Message message, CancellationToken cancellationToken)
        {
            var text = string.Empty;

            text += _commands;

            var suId = await _databaseContext.Configurations.Where(x => x.Label.Equals("su")).FirstOrDefaultAsync(cancellationToken);
            var curentUserIsSu = false;
            if (suId != null)
            {
                curentUserIsSu = suId?.Value.Equals(message?.From?.Id.ToString()) ?? false;
            }

            var user = await _userService.GetUserAsync(message?.From, cancellationToken);

            if (suId is not null && (curentUserIsSu || user.IsAdmin))
            {
                text += "/setgameday - установить день игры\r\n" +
                    "/setgametime - установить день игры\r\n" +
                    "/addsubscriptions - добавить подписчика\r\n" +
                    "/setrules - добавить правили\r\n" +
                    "/setpayment - проставить оплату";
            }

            return await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: text,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
        }

        private async Task<Message> FreeSubscriptions(Message message, CancellationToken cancellationToken)
        {
            var today = DateTime.Now.Date;
            var subscriptionCount = _databaseContext.Subscriptions
                .Where(x => x.IsActive && x.Year.Equals(today.Year) && x.Month.Equals(today.Month))
                .Count();
            var freeSubscription = MAX_SUBSCRIBSION - subscriptionCount;
            var text = $"Осталось {freeSubscription} свободных подписок";

            return await _botClient.SendTextMessageAsync(
                        chatId: message?.Chat?.Id,
                        text: text,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
        }

        private async Task<Message> ListSubscription(Message? message, CancellationToken cancellationToken)
        {
            var today = DateTime.Now.Date;
            var text = $"Список подписчиков на {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(today.Month)} {today.Year}\n\n";
            var i = 1;
            await _databaseContext.Subscriptions
                .Where(x => x.IsActive && x.Year.Equals(today.Year) && x.Month.Equals(today.Month))
                .Include(x => x.User)
                .ForEachAsync(x =>
                {
                    text += $"{i}.) {x?.User?.FirstName} {x?.User?.LastName} {x?.User?.UserName} {x?.User?.Id}\n";
                    i++;
                }, cancellationToken);

            return await _botClient.SendTextMessageAsync(
                        chatId: message?.Chat?.Id,
                        text: text,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
        }

        private async Task<Message> NSPayment(Message message, CancellationToken cancellationToken)
        {
            var today = DateTime.Now.Date;
            var curentUser = await _userService.GetUserAsync(message?.From, cancellationToken);
            var existSubscription = await _databaseContext.Subscriptions
                .Where(x => x.Year.Equals(today.Year) && x.Month.Equals(today.Month) && x.User.Equals(curentUser))
                .FirstOrDefaultAsync();

            var text = string.Empty;
            if (existSubscription is null)
            {
                var newSubscription = new Subscription
                {
                    IsActive = false,
                    Month = today.Month,
                    Year = today.Year,
                    User = curentUser
                };

                _databaseContext.Add(newSubscription);
                _databaseContext.SaveChanges();

                existSubscription = newSubscription;
            }

            _ = await ListSubscription(message, cancellationToken);

            if (existSubscription.IsActive)
            {
                text += "Ваша подписка активна";
            }
            else
            {
                text += $"@{curentUser.UserName} заявка на поддписку приянята, ожидайте одобрение от администратора";
            }

            return await _botClient.SendTextMessageAsync(
                        chatId: message?.Chat?.Id,
                        text: text,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
        }

        private async Task<Message> AddSubscription(Message message, CancellationToken cancellationToken)
        {
            var currentUser = await _userService.GetUserAsync(message?.From, cancellationToken);
            var suUserId = await _databaseContext.Configurations.Where(x => x.Label.Equals("su")).FirstOrDefaultAsync(cancellationToken);
            var suUser = await _databaseContext.Users.FindAsync(long.Parse(suUserId.Value));

            var text = string.Empty;
            if (!await _userService.CheckAdmin(message?.From, cancellationToken))
            {
                if (message.Chat.Type.Equals(ChatType.Supergroup))
                {
                    text += $"@{currentUser?.UserName ?? currentUser?.FirstName + currentUser?.LastName} !!! ";
                }

                text += "У вас нет права на данное действие";
                return await _botClient.SendTextMessageAsync(
                        chatId: message?.Chat?.Id,
                        text: text,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
            }

            var today = DateTime.Now.Date;

            var existId = message?.Text?.Split(" ");
            if (existId?.Length >= 2)
            {
                var subUserId = long.TryParse(existId[1], out var number);
                if (subUserId)
                {
                    var subUser = await _databaseContext.Users.FindAsync(number);
                    if (subUser != null)
                    {
                        var subscriptionsub = subUser?.Subscriptions?.FirstOrDefault(x => x.Year.Equals(today.Year) && x.Month.Equals(today.Month));
                        if (subscriptionsub is not null)
                        {
                            subscriptionsub.IsActive = true;
                            _databaseContext.Subscriptions.Update(subscriptionsub);
                            await _databaseContext.SaveChangesAsync(cancellationToken);


                            var subUserName = subUser?.UserName ?? subUser?.FirstName + subUser?.LastName;
                            text += $"Подписка для @{subUserName} активарована\n";

                            SendMessageToGroup(message, cancellationToken, text);
                        }
                        else
                        {
                            text += $"Пользовтаель с ID {number} не найден\n";
                        }
                    }
                }
            }

            text += $"Заявки на подписки, месяц {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(today.Month)} {today.Year}\n";

            await _databaseContext.Subscriptions
                .Where(x => !x.IsActive && x.Year.Equals(today.Year) && x.Month.Equals(today.Month))
                .ForEachAsync(x =>
                {
                    text += $"/addsubscriptions {x.User?.Id} {x?.User?.FirstName} {x?.User?.LastName} {x?.User?.UserName} \n";
                }, cancellationToken);

            return await _botClient.SendTextMessageAsync(
                        chatId: message?.Chat?.Id,
                        text: text,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
        }

        private async void SendMessageToGroup(Message? message, CancellationToken cancellationToken, string text)
        {
            if (message is not null && message?.Chat?.Id == message?.From?.Id)
            {
                var group = await _databaseContext.Configurations.Where(x => x.Label.Equals(GROUPID)).FirstOrDefaultAsync(cancellationToken);
                if (group is not null)
                {
                    _ = await _botClient.SendTextMessageAsync(
                            chatId: group?.Value,
                            text: text,
                            replyMarkup: new ReplyKeyboardRemove(),
                            cancellationToken: cancellationToken);
                }
            }
        }

        private async Task<Message> SetRules(Message? message, CancellationToken cancellationToken)
        {
            var text = string.Empty;

            if (message.Chat.Type.Equals(ChatType.Supergroup))
            {
                var currentUserName = message?.From?.Username ?? string.Empty;
                if (!String.IsNullOrEmpty(currentUserName))
                {
                    text += $"@{currentUserName} !!! ";
                }
            }

            if (!await _userService.CheckAdmin(message?.From, cancellationToken))
            {
                text += "У вас нет права на данное действие";
                return await _botClient.SendTextMessageAsync(
                        chatId: message?.Chat?.Id,
                        text: text,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
            }

            StepModel step = new()
            {
                UserId = message?.From?.Id ?? 0,
                LastCommand = "setrules"
            };

            _stepService.AddStep(step);

            text = "Напишите правила группы";
            return await _botClient.SendTextMessageAsync(
                        chatId: message?.Chat?.Id,
                        text: text,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
        }

        private async Task<Message> GetRules(Message? message, CancellationToken cancellationToken)
        {
            var rules = await _rulesService.GetRules();
            if (String.IsNullOrEmpty(rules))
            {
                rules = "Правила не заданы";
            }

            return await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: rules,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
        }

        private Task UnknownUpdateHandlerAsync(Update update)
        {
            _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
            return Task.CompletedTask;
        }

        private async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {

            long currentUser = message?.From?.Id ?? 0;
            var usage = "Слава Путину 🥳";

            if (currentUser != 0)
            {
                var step = _stepService.GetStep(currentUser);
                if (step != null)
                {
                    var text = message?.Text ?? "";

                    switch (step.LastCommand)
                    {
                        case "setrules":
                            _ = _rulesService.SetRulesAsync(text);
                            _stepService.RemoveStep(step);
                            usage = "Правила записаны";
                            break;
                    }
                }
            }

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: usage,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        private async Task<Message> SetGameday(Message message, CancellationToken cancellationToken)
        {
            var isAdmin = await _userService.CheckAdmin(message.From, cancellationToken);
            var text = String.Empty;

            if (isAdmin)
            {
                _ = await _gameDay.SetGameDayAsync(3);
                var nextDataGeme = await _gameDay.GetDateGameAsync();
                _ = await _notificationService.SetNotificationAsync(DateOnly.FromDateTime(nextDataGeme.AddDays(-1)));

                text += "Новый день игры задан";
            }
            else
            {
                text += "У вас нет права на данное действие";
            }

            return await _botClient.SendTextMessageAsync(
                    chatId: message?.Chat?.Id,
                    text: text,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
        }
    }
}
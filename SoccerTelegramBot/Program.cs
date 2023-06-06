using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SoccerTelegramBot;
using SoccerTelegramBot.Services;
using Telegram.Bot;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<BotConfiguration>(
            context.Configuration.GetSection(BotConfiguration.Configuration));

        services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    BotConfiguration? botConfig = sp.GetConfiguration<BotConfiguration>();
                    TelegramBotClientOptions options = new(botConfig.BotToken);
                    return new TelegramBotClient(options, httpClient);
                });

        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();

        services.AddTransient<GameDay>();
        services.AddTransient<UserService>();
        services.AddTransient<RulesService>();
        services.AddTransient<NotificationService>();

        services.AddSingleton<StepService>();        

        services.AddDbContext<DatabaseContext>(options => options.UseSqlite(context.Configuration.GetConnectionString("DefaultConnection")));

        services.AddHostedService<SendNotificationService>();
        services.AddHostedService<PollingService>();

    })
    .Build();

await host.RunAsync();

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Extensions;
using MyDiscordApp.Bot;
using MyDiscordApp.Bot.Command;
using MyDiscordApp.Bot.Service;
using ApplicationDI = MyDiscordApp.Application.DependencyInjection;
using InfrastructureDI = MyDiscordApp.Infrastructure.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Register DI services
ApplicationDI.AddApplicationServices(builder.Services);
InfrastructureDI.AddInfrastructureServices(builder.Services, builder.Configuration);

builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents =
    GatewayIntents.Guilds
    | GatewayIntents.GuildMessages
    | GatewayIntents.MessageContent
    | GatewayIntents.GuildVoiceStates
}));

builder.Services.AddLavalink();
builder.Services.ConfigureLavalink(options =>
{
    var lavalinkHostname = builder.Configuration["Lavalink:Hostname"] ?? "localhost";
    var lavalinkPort = builder.Configuration.GetValue<int>("Lavalink:Port") == 0
        ? 2333
        : builder.Configuration.GetValue<int>("Lavalink:Port");
    var lavalinkPassword = builder.Configuration["Lavalink:Password"] ?? "youshallnotpass";

    var baseAddress = $"http://{lavalinkHostname}:{lavalinkPort}";
    var webSocketUri = $"ws://{lavalinkHostname}:{lavalinkPort}/v4/websocket";

    options.BaseAddress = new Uri(baseAddress);
    options.WebSocketUri = new Uri(webSocketUri);
    options.HttpClientName = "Lavalink";
    options.Passphrase = lavalinkPassword;
    options.ReadyTimeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("Lavalink");

builder.Services.AddSingleton(x =>
{
    var client = x.GetRequiredService<DiscordSocketClient>();

    return new InteractionService(client.Rest, new InteractionServiceConfig
    {
        DefaultRunMode = RunMode.Async
    });
});

builder.Services.AddScoped<GeneralModule>();
builder.Services.AddScoped<SongService>();
builder.Services.AddScoped<SongComponentHandler>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<SeedNotifierService>();
builder.Services.AddHostedService<VoiceIdleService>();
builder.Services.AddSingleton<GuildActivityTracker>();
builder.Services.AddHostedService<IdleMessageService>();

var host = builder.Build();
host.Run();

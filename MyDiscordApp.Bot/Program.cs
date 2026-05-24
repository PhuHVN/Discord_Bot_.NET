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
builder.Services.Configure<LavalinkNodeOptions>(options =>
{
    options.BaseAddress = new Uri("http://localhost:2333");
    options.WebSocketUri = new Uri("ws://localhost:2333");
    options.HttpClientName = "Lavalink";
});

builder.Services.AddHttpClient("Lavalink");

builder.Services.AddSingleton(x =>
{
    var client = x.GetRequiredService<DiscordSocketClient>();

    return new InteractionService(client.Rest);
});

builder.Services.AddScoped<GeneralModule>();
builder.Services.AddScoped<SongService>();
builder.Services.AddScoped<SongComponentHandler>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<SeedNotifierService>();
builder.Services.AddSingleton<GuildActivityTracker>();
builder.Services.AddHostedService<IdleMessageService>();

var host = builder.Build();
host.Run();

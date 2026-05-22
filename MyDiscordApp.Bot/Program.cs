using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MyDiscordApp.Bot;
using MyDiscordApp.Bot.Service;
using ApplicationDI = MyDiscordApp.Application.DependencyInjection;
using InfrastructureDI = MyDiscordApp.Infrastructure.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Register DI services
ApplicationDI.AddApplicationServices(builder.Services);
InfrastructureDI.AddInfrastructureServices(builder.Services, builder.Configuration);

builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
}));
builder.Services.AddSingleton(x =>
{
    var client = x.GetRequiredService<DiscordSocketClient>();

    return new InteractionService(client.Rest);
});

builder.Services.AddScoped<GeneralModule>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<SeedNotifierService>();
builder.Services.AddSingleton<GuildActivityTracker>();
builder.Services.AddHostedService<IdleMessageService>();

var host = builder.Build();
host.Run();

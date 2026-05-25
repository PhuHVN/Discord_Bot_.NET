using Discord.WebSocket;

public class VoiceIdleService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;

    public VoiceIdleService(DiscordSocketClient client, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
    {
        _client = client;
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        return Task.Run(async () =>
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CheckIdleVoiceChannelsAsync();
            }
        }, stoppingToken);
    }
    public async Task CheckIdleVoiceChannelsAsync()
    {
        var guildIdValue = _configuration["Discord:GuildId"];
        if (!ulong.TryParse(guildIdValue, out var guildId))
        {
            return;
        }
        var guild = _client.GetGuild(guildId);
        if (guild is null) return;

        foreach (var channel in guild.VoiceChannels)
        {
            if (channel.Users.Count == 0)
            {
                try
                {
                    // Attempt to disconnect any bot users from the idle voice channel                  
                    await channel.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                }
            }
        }
    }
}
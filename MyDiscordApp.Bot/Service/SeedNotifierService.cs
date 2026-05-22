using Discord;
using Discord.WebSocket;

public class SeedNotifierService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    public SeedNotifierService(DiscordSocketClient client, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
    {
        _client = client;
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
    }

    public static TimeSpan GetDelayUntilNextMinute()
    {
        var now = DateTime.UtcNow.AddHours(7);
        var nextFiveMinute = now.AddMinutes(5 - (now.Minute % 5)).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
        return nextFiveMinute - now;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // var talkTask = Task.Run(async () =>
        // {
        //     using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        //     while (await timer.WaitForNextTickAsync(stoppingToken))
        //     {
        //         await TalkAsync();
        //     }
        // }, stoppingToken);
        var seedTask = Task.Run(async () =>
        {

            var delay = GetDelayUntilNextMinute();
            await Task.Delay(delay, stoppingToken);

            await SendSeedInfoAsync();
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SendSeedInfoAsync();
            }
        }, stoppingToken);
        await Task.WhenAll(seedTask);
    }

    private async Task TalkAsync()
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var targetChannelId = _configuration.GetValue<ulong>("Discord:ChatChannelId");
            var talkService = scope.ServiceProvider.GetRequiredService<ITalkService>();
            var channel = _client.GetChannel(targetChannelId) as IMessageChannel;
            if (channel is null) return;
            var response = await talkService.GetTalkResponseAsync();

            await channel.SendMessageAsync(text: $"{response}");
        }
    }
    private async Task SendSeedInfoAsync()
    {
        try
        {
            var _targetChannelId = _configuration.GetValue<ulong>("Discord:BotSeedChannelId");
            var channel = _client.GetChannel(_targetChannelId) as IMessageChannel;

            if (channel is null) return;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var seedService = scope.ServiceProvider.GetRequiredService<ISeedService>();
                var seedInfo = await seedService.GetSeedInfoAsync();

                if (seedInfo is null || seedInfo.Count < 3)
                {
                    return;
                }

                var time = DateTime.UtcNow.AddHours(7).ToString("HH:mm");
                var time2 = DateTime.UtcNow.AddHours(7).AddMinutes(5).ToString("HH:mm");

                var embed = new EmbedBuilder()
                    .WithTitle(":seedling: Seed Information")
                    .WithDescription($"Here is the information about the seeds:\n\n" +
                                     $"**Seed 1:** {seedInfo[0].Seed} x{seedInfo[0].Quantity}\n" +
                                     $"**Seed 2:** {seedInfo[1].Seed} x{seedInfo[1].Quantity}\n" +
                                     $"**Seed 3:** {seedInfo[2].Seed} x{seedInfo[2].Quantity}\n")
                    .WithFooter(footer => footer.Text = $"Time: {time} ~ {time2}")
                    .WithColor(new Color(0x57F287))
                    .Build();

                await channel.SendMessageAsync(text: "@everyone", embed: embed);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error send seed info: {ex.Message}");
        }
    }
}

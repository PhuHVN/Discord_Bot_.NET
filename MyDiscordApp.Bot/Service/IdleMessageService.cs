using Discord.WebSocket;

public class IdleMessageService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly GuildActivityTracker _activityTracker;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _idleTime = TimeSpan.FromMinutes(2);
    private readonly string[] _funnyMessages =
    {
        "Server im quá... có ai còn sống không 😭",
        "10 phút không ai nói gì, bot bắt đầu thấy cô đơn rồi đó 🤖",
        "Alo alo, server này còn thở không mọi người?",
        "Im lặng quá, chắc mọi người đang ngủ rồi 😴",
        "Mọi người đâu hết rồi? Bot cô đơn lắm 🥺",
        "Yooo... còn ai đó không? 👻",
        "Mình lên tiếng xíu thôi mà không ai reply, ngại quá 😞",
        "Server này bây giờ chỉ có mình bot thôi, vậy là thua 🏳️",
        "Nơi này đã chết rồi... hay là mọi người chỉ sống offline? 💀",
        "Ai nấy bận với cuộc sống riêng của mình, bot cô đơn lẻ loi 🎭",
        "Câu im lặng quá lâu rồi, ai nấy đều bận bịu hết hè 📵",
        "Hơi buồn lắm... không ai muốn chát với bot à 😢",
        "Im lặng như tờ, chỉ tiếng gió thôi... 🌊",
        "Bot đang ngồi đây chờ gì? Chờ kỳ tích à? 🙏",
        "Sao lặng thế? Muốn bot chết sao? 💔",
    };

    public IdleMessageService(DiscordSocketClient client, IServiceScopeFactory serviceScopeFactory, GuildActivityTracker activityTracker, IConfiguration configuration)
    {
        _client = client;
        _serviceScopeFactory = serviceScopeFactory;
        _activityTracker = activityTracker;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initialize activity for all guilds on start
        foreach (var guild in _client.Guilds)
        {
            _activityTracker.UpdateActivity(guild.Id);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var guild in _client.Guilds)
            {
                if (!_activityTracker.IsIdle(guild.Id, _idleTime))
                    continue;
                var channelId = _configuration.GetValue<ulong>("Discord:ChatChannelId");
                var channel = guild.GetTextChannel(channelId);
                if (channel is null) continue;
                var random = Random.Shared.Next(_funnyMessages.Length);
                var message = _funnyMessages[random];
                await channel.SendMessageAsync(message);
                _activityTracker.Reset(guild.Id);
            }
        }

    }
}
public class GuildActivityTracker
{
    private readonly Dictionary<ulong, DateTime> _lastActivity = new();

    public void UpdateActivity(ulong guildId)
    {
        _lastActivity[guildId] = DateTime.UtcNow;
    }

    public bool IsIdle(ulong guildId, TimeSpan idleTime)
    {
        if (!_lastActivity.TryGetValue(guildId, out var last))
            return true;  // ✅ Treat uninitialized guilds as idle

        return DateTime.UtcNow - last >= idleTime;
    }

    public void Reset(ulong guildId)
    {
        _lastActivity[guildId] = DateTime.UtcNow;
    }
}
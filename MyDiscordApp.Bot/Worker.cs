using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.Players;
namespace MyDiscordApp.Bot
{
    public class Worker : BackgroundService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _services;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly GuildActivityTracker _activityTracker;
        private readonly IAudioService _lavaNode;


        public Worker(
            DiscordSocketClient client,
            InteractionService interactions,
            IConfiguration configuration,
            ILogger<Worker> logger,
            IServiceProvider services,
            IServiceScopeFactory serviceScopeFactory,
            IAudioService lavaNode,
            GuildActivityTracker activityTracker
            )
        {
            _client = client;
            _interactions = interactions;
            _configuration = configuration;
            _logger = logger;
            _services = services;
            _serviceScopeFactory = serviceScopeFactory;
            _activityTracker = activityTracker;
            _lavaNode = lavaNode;


        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.Log += OnLogAsync;
            _interactions.Log += OnLogAsync;
            _client.Ready += OnReadyAsync;

            _client.InteractionCreated += HandleInteraction;
            _client.MessageReceived += OnMessageReceivedAsync;

            _client.MessageReceived += async (message) =>
            {
                if (message.Channel is SocketTextChannel textChannel)
                {
                    _activityTracker.UpdateActivity(textChannel.Guild.Id);
                }
                ;

            };
            var modules = await _interactions.AddModulesAsync(
                Assembly.GetExecutingAssembly(),
                _services);
            _logger.LogInformation(
                "Loaded {ModuleCount} interaction module(s), {SlashCommandCount} slash command(s)",
                modules.Count(),
                _interactions.SlashCommands.Count);

            var token = _configuration["Discord:Token"];
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Missing configuration: Discord:Token");
            }

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        private async Task OnMessageReceivedAsync(SocketMessage message)
        {
            // Ignore messages from bots
            if (message.Author.IsBot) return;
            var content = message.Content.ToLower();

            // Get guild owner ID
            ulong? ownerId = null;
            if (message.Channel is SocketTextChannel textChannel)
            {
                var guild = textChannel.Guild;
                ownerId = guild?.OwnerId;
            }

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var handleTalkService = scope.ServiceProvider.GetRequiredService<IHandleTalkService>();

                // Show typing indicator before processing
                await message.Channel.TriggerTypingAsync();

                var response = await handleTalkService.HandleTalkAsync(content, message.Author.Id, ownerId);
                if (!string.IsNullOrEmpty(response))
                {
                    await message.Channel.SendMessageAsync(text: $"{response} {MentionUtils.MentionUser(message.Author.Id)}");
                }
            }
        }

        private async Task OnReadyAsync()
        {
            _logger.LogInformation("Bot ready");

            await _client.SetGameAsync("Đang nhớ em...", type: ActivityType.Playing);
            var guildIdValue = _configuration["Discord:GuildId"];
            if (!ulong.TryParse(guildIdValue, out var guildId))
            {
                throw new InvalidOperationException("Missing or invalid configuration: Discord:GuildId");
            }
            await _interactions.RegisterCommandsToGuildAsync(guildId);
            _logger.LogInformation("Registered commands for guild {GuildId}", guildId);

            // Health check Lavalink HTTP endpoint (non-blocking)
            var lavalinkHostname = _configuration["Lavalink:Hostname"] ?? "localhost";
            var lavalinkPort = _configuration.GetValue<int>("Lavalink:Port") == 0
                ? 2333
                : _configuration.GetValue<int>("Lavalink:Port");
            var healthCheckUrl = $"http://{lavalinkHostname}:{lavalinkPort}/version";

            int healthCheckRetries = 15;
            int healthCheckDelay = 1000; // 1 second

            using (var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) })
            {
                for (int i = 0; i < healthCheckRetries; i++)
                {
                    try
                    {
                        var response = await httpClient.GetAsync(healthCheckUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Lavalink HTTP endpoint is healthy at {HealthCheckUrl}", healthCheckUrl);
                            break;
                        }

                        _logger.LogDebug(
                            "Lavalink health check returned {StatusCode} from {HealthCheckUrl}",
                            response.StatusCode,
                            healthCheckUrl);
                    }
                    catch (Exception ex)
                    {
                        if (i < healthCheckRetries - 1)
                        {
                            _logger.LogDebug(ex, "Lavalink health check failed (attempt {Attempt}/{MaxRetries}), retrying in {DelayMs}ms", i + 1, healthCheckRetries, healthCheckDelay);
                            await Task.Delay(healthCheckDelay);
                        }
                        else
                        {
                            _logger.LogWarning(ex, "Lavalink HTTP endpoint not responding after {MaxRetries} attempts. Bot will continue but audio features may not work.", healthCheckRetries);
                        }
                    }
                }
            }

            // Start Lavalink audio service with retry logic (continues even if health check failed)
            int maxRetries = 15;
            int retryDelay = 2000; // 2 seconds
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await _lavaNode.StartAsync();
                    _logger.LogInformation("Lavalink audio service started successfully");

                    // Wait for connection to fully establish
                    await Task.Delay(3000); // 3 seconds for WebSocket to stabilize
                    return;
                }
                catch (Exception ex)
                {
                    if (i < maxRetries - 1)
                    {
                        _logger.LogWarning(ex, "Failed to start Lavalink audio service (attempt {Attempt}/{MaxRetries}), retrying in {DelayMs}ms", i + 1, maxRetries, retryDelay);
                        await Task.Delay(retryDelay);
                    }
                    else
                    {
                        _logger.LogError(ex, "Failed to start Lavalink audio service after {MaxRetries} attempts", maxRetries);
                        throw;
                    }
                }
            }
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client, interaction);

                await _interactions.ExecuteCommandAsync(
                    context,
                    _services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Interaction error");

                if (interaction.Type == InteractionType.ApplicationCommand)
                {
                    await interaction.GetOriginalResponseAsync()
                        .ContinueWith(async msg =>
                            await (await msg).DeleteAsync());
                }
            }
        }

        private Task OnLogAsync(LogMessage log)
        {
            _logger.LogInformation(log.ToString());
            return Task.CompletedTask;
        }
    }
}

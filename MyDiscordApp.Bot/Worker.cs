using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;
using Lavalink4NET;
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

            await _client.SetGameAsync("Call me A...");
            var guildIdValue = _configuration["Discord:GuildId"];
            if (!ulong.TryParse(guildIdValue, out var guildId))
            {
                throw new InvalidOperationException("Missing or invalid configuration: Discord:GuildId");
            }
            await _interactions.RegisterCommandsToGuildAsync(guildId);
            _logger.LogInformation("Registered commands for guild {GuildId}", guildId);
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

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Extensions;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;
using MyDiscordApp.Application.Interface;


namespace MyDiscordApp.Bot.Command
{
    public class GeneralModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IMemeService _memeService;
        private readonly ISeedService _seedService;
        private readonly ITalkService _talkService;
        private readonly IConfiguration _configuration;
        private readonly IAudioService _audioService;

        public GeneralModule(IMemeService memeService, ISeedService seedService
        , ITalkService talkService, IConfiguration configuration, IAudioService audioService, IOptions<LavalinkPlayerOptions> playerOptions)
        {
            _memeService = memeService;
            _seedService = seedService;
            _talkService = talkService;
            _configuration = configuration;
            _audioService = audioService;
        }


        [SlashCommand("help", "General commands for everyone")]
        public async Task Help()
        {
            var embed = new EmbedBuilder()
                .WithTitle(":wrench: Help")
                .WithDescription("Here are the available commands:")
                .AddField("/help", "Show this help message", false)
                .AddField("/ping", "Check bot status", false)
                .AddField("/echo [message]", "Echo back your message", false)
                .AddField("/userinfo [user]", "Get information about a user (defaults to yourself)", false)
                .AddField("/rdmeme", "Get a random meme", false)
                .AddField("/seedinfo", "Get information about the seed", false)
                .WithColor(new Color(0x57F287))
                .Build();
            await RespondAsync(embed: embed);
        }

        [SlashCommand("ping", "Check bot status")]
        public async Task Ping()
        {
            var embed = new EmbedBuilder()
                .WithTitle("Pong")
                .WithDescription("Hello")
                .WithColor(new Color(0x57F287))
                .Build();
            await RespondAsync(embed: embed);
        }

        [SlashCommand("echo", "Echo back your message")]
        public async Task Echo([Summary("message", "The message to echo")] string message)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"🗣️ **{message}**")
                .WithDescription(" Thằng ngu này vừa lên tiếng ➡️")
                .WithThumbnailUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                .WithColor(new Color(0x57F287))
                .WithFooter(footer => footer.Text = $"Requested by {Context.User.Username}#{Context.User.Discriminator}")
                .Build();
            await RespondAsync(embed: embed);
        }


        [SlashCommand("userinfo", "Get information about a user")]
        public async Task UserInfo([Summary("user", "The user to get information about")] IUser? user = null)
        {
            var socketUser = user as SocketGuildUser ?? Context.User as SocketGuildUser;
            if (socketUser is null)
            {
                await RespondAsync("Unable to resolve user information.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithThumbnailUrl(socketUser.GetAvatarUrl() ?? socketUser.GetDefaultAvatarUrl())
                .WithTitle($":identification_card:{socketUser.Username}#{socketUser.Discriminator}")
                .WithColor(new Color(0x57F287))
                .AddField("ID", socketUser.Id.ToString(), true)
                .AddField("Joined Server", socketUser?.JoinedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown", true)
                .AddField("Account Created", socketUser?.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown", true)
                .Build();

            await RespondAsync(embed: embed);
        }
        [SlashCommand("rdmeme", "Get a random meme")]
        public async Task RandomMeme()
        {
            var meme = await _memeService.GetRandomMemeAsync();
            if (meme == null)
            {
                await RespondAsync("Sorry, I couldn't fetch a meme right now. Please try again later.");
                return;
            }
            var embed = new EmbedBuilder()
                .WithTitle(meme.Title)
                .WithImageUrl(meme.Url)
                .WithColor(new Color(0x57F287))
                .Build();
            await RespondAsync(embed: embed);
        }

        [SlashCommand("seedinfo", "Get information about the seed")]
        public async Task SeedInfo()
        {
            var seedInfo = await _seedService.GetSeedInfoAsync();
            var time = DateTime.UtcNow.AddHours(7).ToString("HH:mm");
            var time2 = DateTime.UtcNow.AddHours(7).AddMinutes(5).ToString("HH:mm");
            var embed = new EmbedBuilder()
                .WithTitle(":seedling: Seed Information")
                .WithDescription($"Here is the information about the seeds:\n\n")


                .WithDescription($"Here is the information about the seeds:\n\n" +
                                 $"**Seed 1:** {seedInfo[0].Seed} x{seedInfo[0].Quantity}\n" +
                                 $"**Seed 2:** {seedInfo[1].Seed} x{seedInfo[1].Quantity}\n" +
                                 $"**Seed 3:** {seedInfo[2].Seed} x{seedInfo[2].Quantity} \n"
                                )
                .WithFooter(footer => footer.Text = $"Time: {time} ~ {time2}")
                .WithColor(new Color(0x57F287))
                .Build();
            await RespondAsync(text: "@everyone", embed: embed);
        }
        [SlashCommand("talk", "Talk with the bot")]
        public async Task Talk()
        {
            var response = await _talkService.GetTalkResponseAsync();
            var embed = new EmbedBuilder()
                .WithDescription(response)
                .WithColor(new Color(0x57F287))
                .Build();
            await RespondAsync(embed: embed);
        }
        [SlashCommand("checkowner", "Check if you are the bot owner")]
        public async Task CheckOwner()
        {
            var guildId = _configuration.GetValue<ulong>("Discord:GuildId");
            var guild = Context.Client.GetGuild(guildId);
            var ownerId = guild?.OwnerId;
            if (Context.User.Id == ownerId)
            {
                await RespondAsync("You are the bot owner!");
            }
            else
            {
                await RespondAsync("You are not the bot owner.");
            }
        }

    }
}
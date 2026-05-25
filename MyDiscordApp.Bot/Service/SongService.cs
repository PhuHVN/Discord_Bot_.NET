using System.Collections.Concurrent;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Events.Players;
using Lavalink4NET.Extensions;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;

namespace MyDiscordApp.Bot.Service
{
    public class SongService : InteractionModuleBase<SocketInteractionContext>
    {
        private static int _trackEndedSubscribed;
        private readonly IAudioService _audioService;
        private readonly ILogger<SongService> _logger;

        public SongService(IAudioService audioService, ILogger<SongService> logger)
        {
            _audioService = audioService;
            _logger = logger;

            if (Interlocked.Exchange(ref _trackEndedSubscribed, 1) == 0)
            {
                _audioService.TrackEnded += HandleTrackEndedAsync;
            }
        }

        private static async Task HandleTrackEndedAsync(object? sender, TrackEndedEventArgs args)
        {
            if (!SongQueue.TryDequeue(args.Player.GuildId, out var nextTrack))
            {
                return;
            }

            var reason = args.Reason.ToString();
            if (!string.Equals(reason, "Finished", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(reason, "LoadFailed", StringComparison.OrdinalIgnoreCase))
            {
                SongQueue.EnqueueFront(args.Player.GuildId, nextTrack);
                return;
            }

            await args.Player.PlayAsync(nextTrack);
        }

        [SlashCommand("join", "Join your voice channel")]
        public async Task Join()
        {
            var user = Context.User as IGuildUser;
            if (user?.VoiceChannel == null)
            {
                await RespondAsync("Join a voice channel first.", ephemeral: true);
                return;
            }

            try
            {
                await _audioService.Players.JoinAsync(
                    guildId: Context.Guild.Id,
                    voiceChannelId: user.VoiceChannel.Id);
            }
            catch (InvalidOperationException ex) when (IsLavalinkNotReadyException(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Lavalink is not ready while joining guild {GuildId} and voice channel {VoiceChannelId}",
                    Context.Guild.Id,
                    user.VoiceChannel.Id);

                await RespondAsync("Lavalink is still connecting. Please try again in a few seconds.", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("Connected")
                .WithDescription($"Joined **{user.VoiceChannel.Name}**. Use `/play` to add music.")
                .WithColor(SongUi.SuccessColor)
                .WithThumbnailUrl(Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
                .Build();

            await RespondAsync(embed: embed, components: SongUi.BuildControls(SongQueue.IsPaused(Context.Guild.Id)));
        }

        [SlashCommand("play", "Play a song from YouTube")]
        public async Task Play([Summary("query", "Song name or URL")] string query)
        {
            var user = Context.User as IGuildUser;
            if (user?.VoiceChannel == null)
            {
                await RespondAsync("You need to be in a voice channel first.", ephemeral: true);
                return;
            }

            ILavalinkPlayer player;

            try
            {
                player = await _audioService.Players.GetPlayerAsync(Context.Guild.Id)
                    ?? await _audioService.Players.JoinAsync(
                        guildId: Context.Guild.Id,
                        voiceChannelId: user.VoiceChannel.Id);
            }
            catch (InvalidOperationException ex) when (IsLavalinkNotReadyException(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Lavalink is not ready while playing in guild {GuildId} and voice channel {VoiceChannelId}",
                    Context.Guild.Id,
                    user.VoiceChannel.Id);

                await RespondAsync("Lavalink is still connecting. Please try again in a few seconds.", ephemeral: true);
                return;
            }

            var track = await _audioService.Tracks.LoadTrackAsync(query, TrackSearchMode.YouTube);
            if (track == null)
            {
                await RespondAsync("No matches found for your query.", ephemeral: true);
                return;
            }

            if (player.CurrentTrack != null)
            {
                var position = SongQueue.Enqueue(Context.Guild.Id, track);

                var queueEmbed = SongUi.BuildQueuedEmbed(track, position, SongQueue.Count(Context.Guild.Id));
                await RespondAsync(embed: queueEmbed, components: SongUi.BuildControls(SongQueue.IsPaused(Context.Guild.Id)));
                return;
            }

            await player.PlayAsync(track);
            SongQueue.SetPaused(Context.Guild.Id, isPaused: false);

            var playEmbed = SongUi.BuildNowPlayingEmbed(track, SongQueue.Count(Context.Guild.Id));
            await RespondAsync(embed: playEmbed, components: SongUi.BuildControls(SongQueue.IsPaused(Context.Guild.Id)));
        }



        [SlashCommand("skip", "Skip the current song")]
        public async Task Skip()
        {
            var player = await _audioService.Players.GetPlayerAsync(Context.Guild.Id);
            if (player?.CurrentTrack == null)
            {
                await RespondAsync("No song is currently playing.", ephemeral: true);
                return;
            }

            var embed = await SongActions.SkipAsync(player, Context.Guild.Id);
            await RespondAsync(embed: embed, components: SongUi.BuildControls(SongQueue.IsPaused(Context.Guild.Id)));
        }

        [SlashCommand("queue", "Show the current music queue")]
        public async Task Queue()
        {
            var player = await _audioService.Players.GetPlayerAsync(Context.Guild.Id);
            var embed = SongUi.BuildQueueEmbed(player?.CurrentTrack, SongQueue.Snapshot(Context.Guild.Id));

            await RespondAsync(embed: embed, components: SongUi.BuildControls(SongQueue.IsPaused(Context.Guild.Id)), ephemeral: true);
        }

        private static bool IsLavalinkNotReadyException(InvalidOperationException exception)
        {
            return exception.Message.Contains("session identifier", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("node has been established", StringComparison.OrdinalIgnoreCase)
                || exception.Message.Contains("StartAsync", StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static class SongActions
    {
        public static async Task<Embed> SkipAsync(ILavalinkPlayer player, ulong guildId)
        {
            if (SongQueue.TryDequeue(guildId, out var nextTrack))
            {
                await player.PlayAsync(nextTrack);
                SongQueue.SetPaused(guildId, isPaused: false);
                return SongUi.BuildSkippedEmbed(nextTrack, SongQueue.Count(guildId));
            }

            await player.StopAsync();
            SongQueue.SetPaused(guildId, isPaused: false);
            return new EmbedBuilder()
                .WithTitle("Skipped")
                .WithDescription("Queue is empty.")
                .WithColor(SongUi.WarningColor)
                .Build();
        }

        public static async Task<Embed> TogglePauseAsync(ILavalinkPlayer player, ulong guildId)
        {
            if (SongQueue.IsPaused(guildId))
            {
                await player.ResumeAsync();
                SongQueue.SetPaused(guildId, isPaused: false);
                return new EmbedBuilder()
                    .WithTitle("▶️ Resumed")
                    .WithDescription("Playback has been resumed.")
                    .WithColor(SongUi.SuccessColor)
                    .Build();
            }

            await player.PauseAsync();
            SongQueue.SetPaused(guildId, isPaused: true);
            return new EmbedBuilder()
                .WithTitle("⏸️ Paused")
                .WithDescription("Playback has been paused.")
                .WithColor(SongUi.WarningColor)
                .Build();
        }

        public static async Task<Embed> StopAsync(ILavalinkPlayer player, ulong guildId)
        {
            SongQueue.Clear(guildId);
            SongQueue.SetPaused(guildId, isPaused: false);
            await player.StopAsync();

            return new EmbedBuilder()
                .WithTitle("⏹️ Stopped")
                .WithDescription("Playback stopped and queue cleared.")
                .WithColor(SongUi.DangerColor)
                .Build();
        }
    }

    internal static class SongQueue
    {
        private static readonly ConcurrentDictionary<ulong, Queue<LavalinkTrack>> Queues = new();
        private static readonly ConcurrentDictionary<ulong, bool> PausedStates = new();

        public static int Enqueue(ulong guildId, LavalinkTrack track)
        {
            var queue = Queues.GetOrAdd(guildId, _ => new Queue<LavalinkTrack>());

            lock (queue)
            {
                queue.Enqueue(track);
                return queue.Count;
            }
        }

        public static void EnqueueFront(ulong guildId, LavalinkTrack track)
        {
            var queue = Queues.GetOrAdd(guildId, _ => new Queue<LavalinkTrack>());

            lock (queue)
            {
                var current = queue.ToArray();
                queue.Clear();
                queue.Enqueue(track);

                foreach (var item in current)
                {
                    queue.Enqueue(item);
                }
            }
        }

        public static bool TryDequeue(ulong guildId, out LavalinkTrack track)
        {
            var queue = Queues.GetOrAdd(guildId, _ => new Queue<LavalinkTrack>());

            lock (queue)
            {
                if (queue.Count == 0)
                {
                    track = null!;
                    return false;
                }

                track = queue.Dequeue();
                return true;
            }
        }

        public static IReadOnlyList<LavalinkTrack> Snapshot(ulong guildId)
        {
            var queue = Queues.GetOrAdd(guildId, _ => new Queue<LavalinkTrack>());

            lock (queue)
            {
                return queue.ToArray();
            }
        }

        public static int Count(ulong guildId)
        {
            var queue = Queues.GetOrAdd(guildId, _ => new Queue<LavalinkTrack>());

            lock (queue)
            {
                return queue.Count;
            }
        }

        public static void Clear(ulong guildId)
        {
            var queue = Queues.GetOrAdd(guildId, _ => new Queue<LavalinkTrack>());

            lock (queue)
            {
                queue.Clear();
            }
        }

        public static bool IsPaused(ulong guildId)
        {
            return PausedStates.TryGetValue(guildId, out var isPaused) && isPaused;
        }

        public static void SetPaused(ulong guildId, bool isPaused)
        {
            PausedStates.AddOrUpdate(guildId, isPaused, (_, _) => isPaused);
        }
    }

    internal static class SongUi
    {
        public static readonly Color SuccessColor = new(0x57F287);
        public static readonly Color WarningColor = new(0xFEE75C);
        public static readonly Color DangerColor = new(0xED4245);
        public static readonly Color InfoColor = new(0x5865F2);

        public static MessageComponent BuildControls(bool isPaused)
        {
            return new ComponentBuilder()
                .WithButton(isPaused ? "▶️ Resume" : "⏸️ Pause", "btn_pause", ButtonStyle.Secondary)
                .WithButton("⏭️ Skip", "btn_skip", ButtonStyle.Primary)
                .WithButton("📋 Queue", "btn_queue", ButtonStyle.Secondary)
                .WithButton("⏹️ Stop", "btn_stop", ButtonStyle.Danger)
                .WithButton("🎲 Random", "btn_random", ButtonStyle.Success)
                .Build();
        }

        public static Embed BuildNowPlayingEmbed(LavalinkTrack track, int queueCount)
        {
            return new EmbedBuilder()
                .WithTitle("🎵 Now Playing")
                .WithDescription($"**{track.Title}**")
                .AddField("👤 Author", track.Author ?? "Unknown", true)
                .AddField("⏱️ Duration", FormatDuration(track.Duration), true)
                .AddField("📊 Queue", $"{queueCount} waiting", true)
                .WithColor(SuccessColor)
                .Build();
        }

        public static Embed BuildQueuedEmbed(LavalinkTrack track, int position, int queueCount)
        {
            return new EmbedBuilder()
                .WithTitle("➕ Added to Queue")
                .WithDescription($"**{track.Title}**")
                .AddField("#️⃣ Position", $"#{position}", true)
                .AddField("⏱️ Duration", FormatDuration(track.Duration), true)
                .AddField("📊 Queue", $"{queueCount} waiting", true)
                .WithColor(InfoColor)
                .Build();
        }

        public static Embed BuildSkippedEmbed(LavalinkTrack nextTrack, int queueCount)
        {
            return new EmbedBuilder()
                .WithTitle("⏭️ Skipped")
                .WithDescription($"Now playing **{nextTrack.Title}**")
                .AddField("⏱️ Duration", FormatDuration(nextTrack.Duration), true)
                .AddField("📊 Queue", $"{queueCount} waiting", true)
                .WithColor(WarningColor)
                .Build();
        }

        public static Embed BuildQueueEmbed(LavalinkTrack? currentTrack, IReadOnlyList<LavalinkTrack> queue)
        {
            var description = currentTrack == null
                ? "Nothing is playing right now."
                : $"Now playing: **{currentTrack.Title}**";

            var embed = new EmbedBuilder()
                .WithTitle("📋 Music Queue")
                .WithDescription(description)
                .WithColor(InfoColor);

            if (queue.Count == 0)
            {
                embed.AddField("⬇️ Up Next", "Queue is empty.");
                return embed.Build();
            }

            var tracks = queue
                .Take(10)
                .Select((track, index) => $"{index + 1}. **{track.Title}** - {FormatDuration(track.Duration)}");

            embed.AddField("⬇️ Up Next", string.Join("\n", tracks));

            if (queue.Count > 10)
            {
                embed.WithFooter($"Showing 10 of {queue.Count} queued tracks");
            }

            return embed.Build();
        }

        private static string FormatDuration(TimeSpan duration)
        {
            return duration.TotalHours >= 1
                ? $"`{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}`"
                : $"`{duration.Minutes:D2}:{duration.Seconds:D2}`";
        }
    }
}

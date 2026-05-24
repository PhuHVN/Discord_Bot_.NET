using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Rest.Entities.Tracks;

namespace MyDiscordApp.Bot.Service
{
    public class SongComponentHandler : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IAudioService _audioService;

        public SongComponentHandler(IAudioService audioService)
        {
            _audioService = audioService;
        }

        [ComponentInteraction("btn_skip")]
        public async Task HandleSkipButton()
        {
            var player = await _audioService.Players.GetPlayerAsync(Context.Guild.Id);
            if (player?.CurrentTrack == null)
            {
                await RespondAsync("No song is currently playing.", ephemeral: true);
                return;
            }

            var embed = await SongActions.SkipAsync(player, Context.Guild.Id);
            await RespondAsync(embed: embed, components: SongUi.BuildControls(SongQueue.IsPaused(Context.Guild.Id)), ephemeral: true);
        }

        [ComponentInteraction("btn_pause")]
        public async Task HandlePauseButton()
        {
            var player = await _audioService.Players.GetPlayerAsync(Context.Guild.Id);
            if (player?.CurrentTrack == null)
            {
                await RespondAsync("No song is currently playing.", ephemeral: true);
                return;
            }

            var embed = await SongActions.TogglePauseAsync(player, Context.Guild.Id);
            await RespondAsync(embed: embed, components: SongUi.BuildControls(SongQueue.IsPaused(Context.Guild.Id)), ephemeral: true);
        }

        [ComponentInteraction("btn_play")]
        public async Task HandlePlayButton()
        {
            var player = await _audioService.Players.GetPlayerAsync(Context.Guild.Id);
            if (player?.CurrentTrack == null)
            {
                await RespondAsync("No song is currently playing.", ephemeral: true);
                return;
            }

            if (!SongQueue.IsPaused(Context.Guild.Id))
            {
                var alreadyPlayingEmbed = new EmbedBuilder()
                    .WithTitle("Already Playing")
                    .WithDescription($"**{player.CurrentTrack.Title}** is already playing.")
                    .WithColor(SongUi.SuccessColor)
                    .Build();

                await RespondAsync(embed: alreadyPlayingEmbed, components: SongUi.BuildControls(isPaused: false), ephemeral: true);
                return;
            }

            var embed = await SongActions.TogglePauseAsync(player, Context.Guild.Id);
            await RespondAsync(embed: embed, components: SongUi.BuildControls(SongQueue.IsPaused(Context.Guild.Id)), ephemeral: true);
        }

        [ComponentInteraction("btn_stop")]
        public async Task HandleStopButton()
        {
            var player = await _audioService.Players.GetPlayerAsync(Context.Guild.Id);
            if (player == null)
            {
                await RespondAsync("No player found for this guild.", ephemeral: true);
                return;
            }

            var embed = await SongActions.StopAsync(player, Context.Guild.Id);
            await RespondAsync(embed: embed, components: SongUi.BuildControls(isPaused: false), ephemeral: true);
        }

        [ComponentInteraction("btn_queue")]
        public async Task HandleQueueButton()
        {
            var player = await _audioService.Players.GetPlayerAsync(Context.Guild.Id);
            var embed = SongUi.BuildQueueEmbed(player?.CurrentTrack, SongQueue.Snapshot(Context.Guild.Id));

            await RespondAsync(embed: embed, components: SongUi.BuildControls(SongQueue.IsPaused(Context.Guild.Id)), ephemeral: true);
        }

        [ComponentInteraction("btn_play_next")]
        public async Task HandlePlayNextButton()
        {
            await HandleSkipButton();
        }
        private readonly string[] randomSongs = new[]
                {
            "https://www.youtube.com/watch?v=rDpJfmBI9xQ&list=RDrDpJfmBI9xQ&start_radio=1",
            "https://www.youtube.com/watch?v=sxQCA_k_PcM&list=RDsxQCA_k_PcM&start_radio=1",
            "https://www.youtube.com/watch?v=1T8uv7h4dm4&list=RDsxQCA_k_PcM&index=3"
        };

        [ComponentInteraction("btn_random")]
        public async Task HandleRandomButton()
        {
            var player = await _audioService.Players.GetPlayerAsync(Context.Guild.Id);
            if (player == null)
            {
                await RespondAsync("No player found. Join a voice channel first.", ephemeral: true);
                return;
            }

            var randomIndex = System.Random.Shared.Next(randomSongs.Length);
            var randomSongUrl = randomSongs[randomIndex];

            var track = await _audioService.Tracks.LoadTrackAsync(randomSongUrl, TrackSearchMode.YouTube);
            if (track == null)
            {
                await RespondAsync("Failed to load random song.", ephemeral: true);
                return;
            }

            if (player.CurrentTrack != null)
            {
                var position = SongQueue.Enqueue(Context.Guild.Id, track);
                var queueEmbed = SongUi.BuildQueuedEmbed(track, position, SongQueue.Count(Context.Guild.Id));
                await RespondAsync(embed: queueEmbed, components: SongUi.BuildControls(SongQueue.IsPaused(Context.Guild.Id)), ephemeral: true);
                return;
            }

            await player.PlayAsync(track);
            SongQueue.SetPaused(Context.Guild.Id, isPaused: false);

            var playEmbed = SongUi.BuildNowPlayingEmbed(track, SongQueue.Count(Context.Guild.Id));
            await RespondAsync(embed: playEmbed, components: SongUi.BuildControls(SongQueue.IsPaused(Context.Guild.Id)), ephemeral: true);
        }
    }
}

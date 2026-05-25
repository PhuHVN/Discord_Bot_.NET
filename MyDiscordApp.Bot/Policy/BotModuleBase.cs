using Discord;
using Discord.Interactions;
using Discord.WebSocket;

public abstract class BotModuleBase : InteractionModuleBase<SocketInteractionContext>
{
    protected readonly IConfiguration _configuration;

    public BotModuleBase(IConfiguration configuration)
    {
        _configuration = configuration;

    }
    protected async Task<bool> EnsureMusicVoiceChannelAsync()
    {
        var channelChatId = _configuration.GetValue<ulong>("Discord:MusicChannelId");
        if (Context.Channel.Id != channelChatId)
        {
            await RespondAsync($"You can only use music commands in the music channel.", ephemeral: true);
            return false;
        }
        return true;
    }
    protected async Task<bool> EnsureChatChannelAsync()
    {
        var channelChatId = _configuration.GetValue<ulong>("Discord:ChatChannelId");
        if (Context.Channel.Id != channelChatId)
        {
            await RespondAsync($"You can only use this command in the chat channel.", ephemeral: true);
            return false;
        }
        return true;

    }
}
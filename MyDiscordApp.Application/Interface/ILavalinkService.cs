public interface ILavalinkService
{
    Task SkipAsync(ulong guildId);
    Task PlayAsync(ulong guildId, string query);
    Task LeaveAsync(ulong guildId);
    Task StopAsync(ulong guildId);
}
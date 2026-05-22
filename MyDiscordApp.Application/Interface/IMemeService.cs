namespace MyDiscordApp.Application.Interface
{
    public interface IMemeService
    {
        Task<MemeDto?> GetRandomMemeAsync();
    }
}

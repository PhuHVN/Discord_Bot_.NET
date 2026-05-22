public interface IHandleTalkService
{
    Task<string> HandleTalkAsync(string message, ulong? userId = null, ulong? ownerId = null);
}
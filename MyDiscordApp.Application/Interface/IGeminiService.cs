public interface IGeminiService
{
    Task<GeminiDto> GenerateReplyAsync(string message);
}
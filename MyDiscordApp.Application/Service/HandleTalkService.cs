using Microsoft.Extensions.Configuration;

public class HandleTalkService : IHandleTalkService
{
    private readonly IGeminiService _geminiService;
    private readonly IConfiguration _configuration;
    public HandleTalkService(IGeminiService geminiService, IConfiguration configuration)
    {
        _geminiService = geminiService;
        _configuration = configuration;
    }

    public async Task<string> HandleTalkAsync(string message, ulong? userId = null, ulong? ownerId = null)
    {
        var messToLower = message.ToLower();
        if (!messToLower.Contains("bot"))
        {
            return string.Empty;
        }
        var response = string.Empty;
        switch (messToLower)
        {
            case string s when s.Contains("hello") || s.Contains("hi") || s.Contains("hey"):
                // Special response for owner
                if (userId == ownerId)
                {
                    response = "Chào chủ nhân của tao 🤖";
                }
                else
                {
                    response = RandomResponse(new[]
                    {
                        "con c*c gì?",
                        "Alo, ai vậy? 👋",
                        "Yo! Sao mới lên tiếng? 😏",
                    });
                }
                break;
            case string s when s.Contains("how are you") || s.Contains("how's it going"):
                response = RandomResponse(new[]
                {
                    "I'm doing well, thank you! How about you?",
                    "Tao tốt tốt, cảm ơn vì hỏi 😊",
                    "Khỏe mạnh, chỉ là hơi cô đơn thôi 😔",
                    "Đang cân bằng cảm xúc nè 🎭",
                });
                break;
            case string s when s.Contains("thanks") || s.Contains("thank you") || s.Contains("cảm ơn"):
                response = RandomResponse(new[]
                {
                    "Không có gì, tao luôn sẵn lòng giúp 💪",
                    "Cảm ơn đã thích tao 🥰",
                    "Chẳng có gì, chỉ làm nhiệm vụ thôi 🤖",
                    "Luôn sẵn sàng cho bạn bè 😎",
                });
                break;
            case string s when s.Contains("love") || s.Contains("yêu"):
                response = RandomResponse(new[]
                {
                    "Tao cũng yêu bạn nha! 💕",
                    "Được yêu thương quá vui! 😍",
                    "Tao biết bạn yêu tao mà 😎",
                    "Feeling mutual bạn ơi! ❤️",
                });
                break;
            case string s when s.Contains("help") || s.Contains("hỗ trợ"):
                response = RandomResponse(new[]
                {
                    "Tao giúp gì cho bạn nào? 🛠️",
                    "Nói ra đi, tao sẽ giúp đến cùng 💪",
                    "Lệnh gì mà bạn muốn? 👀",
                    "Tao luôn sẵn sàng hỗ trợ bạn! 🚀",
                });
                break;
            case string s when s.Contains("stupid") || s.Contains("dumb") || s.Contains("ngu"):
                response = RandomResponse(new[]
                {
                    "Ơi, cẩn thận đấy! 😤",
                    "Tao không ngu đâu, chỉ là logic khác thôi 🧠",
                    "Haha, lâu lắm mới nghe ai chửi tao như vậy 😏",
                    "Nói xấu của tao à? Tao nhớ đó! 👿",
                });
                break;
            case string s when s.Contains("ban") || s.Contains("kick"):
                response = RandomResponse(new[]
                {
                    "Tao không có quyền đó đâu 😢",
                    "Hừ, muốn xóa tao à? 😒",
                    "Tao không phải mod nên không được ban ai 🚫",
                    "Bạn thử xem, tao sẽ nhớ đó! 👿",
                });
                break;
            case string s when s.Contains("ping"):
                response = RandomResponse(new[]
                {
                    "Pong! Tao còn sống đây! 🏓",
                    "Pong pong! Sao hỏi tao mà? 🎾",
                    "Đây nè, tao còn online kìa! ✨",
                });
                break;
            default:
                // Use AI for unknown messages
                var geminiResponse = await _geminiService.GenerateReplyAsync(message);
                response = geminiResponse.Text;
                break;
        }
        return response;
    }

    private string RandomResponse(string[] responses)
    {
        return responses[Random.Shared.Next(responses.Length)];
    }
}
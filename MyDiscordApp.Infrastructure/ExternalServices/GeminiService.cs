using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;

    private readonly string _systemPrompt =
        "Bot Discord tiếng Việt kiểu bạn bè gaming.Cà khịa, chửi thề, toxic.Reply ngắn dưới 15 từ.không lịch sự.";

    public GeminiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GeminiDto> GenerateReplyAsync(string message)
    {
        try
        {
            var apiKey = _configuration.GetValue<string>("Gemini:ApiKey");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("Gemini API key is not configured");
                return new GeminiDto("Bot chưa config Gemini API key.");
            }

            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";

            var body = new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = _systemPrompt }
                    }
                },

                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = message }
                        }
                    }
                },
                generationConfig = new
                {

                    temperature = 1.2,

                }


            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
            var requestJson = JsonSerializer.Serialize(body, options);

            using var requestContent = new StringContent(
                requestJson,
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(url, requestContent);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Gemini API error ({StatusCode}): {Content}",
                    response.StatusCode,
                    responseContent
                );

                return new GeminiDto("Gemini lỗi rồi, chắc tôi cũng đang lag 😭");
            }

            var text = ExtractGeminiText(responseContent);

            if (string.IsNullOrWhiteSpace(text))
                return new GeminiDto("Gemini trả lời trống trơn luôn 😭");

            return new GeminiDto(text.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            return new GeminiDto("Bot lỗi rồi, đừng dí nữa 😭");
        }
    }

    private static string ExtractGeminiText(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates))
            {
                System.Console.WriteLine("❌ No 'candidates' property");
                return "";
            }

            if (candidates.ValueKind != System.Text.Json.JsonValueKind.Array || candidates.GetArrayLength() == 0)
            {
                System.Console.WriteLine($"❌ candidates is not array or empty. ValueKind: {candidates.ValueKind}, Length: {candidates.GetArrayLength()}");
                return "";
            }

            var firstCandidate = candidates[0];
            if (!firstCandidate.TryGetProperty("content", out var content))
            {
                System.Console.WriteLine("❌ No 'content' in first candidate");
                return "";
            }

            if (!content.TryGetProperty("parts", out var parts) || parts.ValueKind != System.Text.Json.JsonValueKind.Array || parts.GetArrayLength() == 0)
            {
                System.Console.WriteLine($"❌ 'parts' missing/invalid. ValueKind: {parts.ValueKind}");
                return "";
            }

            var firstPart = parts[0];
            if (!firstPart.TryGetProperty("text", out var textElement))
            {
                System.Console.WriteLine("❌ No 'text' in first part");
                return "";
            }

            var result = textElement.GetString() ?? "";
            System.Console.WriteLine($"✅ Extracted: {result}");
            return result;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Exception: {ex.Message}");
            return "";
        }
    }
}
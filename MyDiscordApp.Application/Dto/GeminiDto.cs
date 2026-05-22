using System.Text.Json.Serialization;

public class GeminiDto
{
    [JsonPropertyName("candidates")]
    public List<Candidate>? Candidates { get; set; }

    [JsonIgnore]
    public string Text
    {
        get
        {
            if (Candidates?.Count > 0)
            {
                var text = Candidates[0].Content?.Parts?.FirstOrDefault()?.Text;
                return !string.IsNullOrEmpty(text) ? text : "Sorry, I couldn't get a response.";
            }
            return _customText ?? "Sorry, I couldn't get a response.";
        }
    }

    private string? _customText;

    public GeminiDto() { }

    public GeminiDto(string text)
    {
        _customText = text;
    }
}

public class Candidate
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }
}

public class Content
{
    [JsonPropertyName("parts")]
    public List<Part>? Parts { get; set; }
}

public class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
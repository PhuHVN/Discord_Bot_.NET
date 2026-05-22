public class RandomTalkService : ITalkService
{

    private readonly List<string> responses = new List<string>
    {
        "Hello! How can I assist you today?",
        "Hi there! What can I do for you?",
        "Greetings! Need any help?",
        "Hey! How's it going?",
        "Hello! What would you like to talk about?"
    };

    public Task<string> GetTalkResponseAsync()
    {
        var random = new Random();
        int index = random.Next(responses.Count);
        return Task.FromResult(responses[index]);
    }
}
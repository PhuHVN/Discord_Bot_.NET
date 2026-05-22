using MyDiscordApp.Application.Interface;
using System.Net.Http.Json;

public class MemeService : IMemeService
{
    private readonly HttpClient _httpClient;
    public MemeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<MemeDto?> GetRandomMemeAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<MemeDto>(
             "https://meme-api.com/gimme/dankmemes");

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching meme: {ex.Message}");
            return null;
        }
    }
}
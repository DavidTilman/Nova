using System.Text.Json;

namespace Nova.APIs;
public static class Trading212
{
    private static readonly string apiKey = Environment.GetEnvironmentVariable("NOVA-Trading212APIKey")!;
    static HttpClient HttpClient { get; set; } = new HttpClient();

    public static async Task<List<Trading212Position>> GetPositionsAsync()
    {
        if (LastCallTime != null && (DateTime.UtcNow - LastCallTime) < TimeSpan.FromMinutes(15))
            return System.Text.Json.JsonSerializer.Deserialize<List<Trading212Position>>(LastCallContent!) ?? [];

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://live.trading212.com/api/v0/equity/portfolio");
        request.Headers.Add("Authorization", apiKey);
        HttpResponseMessage response = HttpClient.Send(request);
        string responseContent = await response.Content.ReadAsStringAsync();
        List<Trading212Position> trading212Positions = System.Text.Json.JsonSerializer.Deserialize<List<Trading212Position>>(responseContent) ?? [];

        LastCallTime = DateTime.UtcNow;
        LastCallContent = responseContent;

        return trading212Positions;
    }

    public static readonly List<Dictionary<string, JsonElement>> InstrumentData = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(File.ReadAllText("C:\\Users\\DTilm\\source\\repos\\Nova\\Nova\\APIs\\instrument_data.json"))!;

    private static DateTime? LastCallTime = null;

    private static string? LastCallContent = null; 
}
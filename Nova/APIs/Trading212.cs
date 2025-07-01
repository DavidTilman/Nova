using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.APIs;
public static class Trading212
{
    static HttpClient HttpClient { get; set; } = new HttpClient();

    public static async Task<List<Trading212Position>> GetPositions()
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://live.trading212.com/api/v0/equity/portfolio");
        request.Headers.Add("Authorization", APIConfig.Trading212ApiKey);
        HttpResponseMessage response = HttpClient.Send(request);
        Debug.WriteLine($"Response: {response.StatusCode}");
        string responseContent = await response.Content.ReadAsStringAsync();
        List<Trading212Position> trading212Positions = System.Text.Json.JsonSerializer.Deserialize<List<Trading212Position>>(responseContent) ?? new List<Trading212Position>();
        return trading212Positions;
    }
}

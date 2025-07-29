using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nova.APIs;
public class Kraken
{
    static HttpClient HttpClient { get; set; } = new HttpClient();

    public static async Task<List<KrakenPosition>> GetBalanceAsync()
    {
        string urlPath = "/0/private/Balance";
        string apiUrl = "https://api.kraken.com" + urlPath;

        string nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        string postData = $"nonce={nonce}";

        // Generate API-Sign
        string signature = APIConfig.CreateSignature(urlPath, nonce, postData);

        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded")
        };
        request.Headers.Add("API-Key", APIConfig.KrakenApiKey);
        request.Headers.Add("API-Sign", signature);


        var response = await HttpClient.SendAsync(request);
        string responseJson = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(responseJson);
        var resultElement = doc.RootElement.GetProperty("result");

        var dict = new Dictionary<string, decimal>();
        foreach (var property in resultElement.EnumerateObject())
        {
            if (decimal.TryParse(property.Value.GetString(), out var value))
            {
                dict[property.Name] = value;
            }
        }

        List<KrakenPosition> positions = new List<KrakenPosition>();

        foreach (var pair in dict)
        {
            positions.Add(new KrakenPosition
            {
                Currency = pair.Key,
                Quantity = Convert.ToDouble(pair.Value)
            });
        }

        return positions;
    }
}

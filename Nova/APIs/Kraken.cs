using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Nova.Secrets;

namespace Nova.APIs;
public class Kraken
{
    static HttpClient HttpClient { get; set; } = new HttpClient();
    public static async Task<List<KrakenPosition>> GetBalanceAsync()
    {
        Debug.WriteLine("Fetching Kraken balance data...");
        if (Cache.LastCallContent != null)
        {
            Debug.WriteLine("Using cached Kraken balance data.");
            return Cache.LastCallContent;
        }

        string urlPath = "/0/private/Balance";
        string apiUrl = "https://api.kraken.com" + urlPath;

        string nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        string postData = $"nonce={nonce}";

        // Generate API-Sign
        string signature = KrakenConfig.GetKrakenSignature(urlPath, nonce, postData);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded")
        };
        request.Headers.Add("API-Key", KrakenConfig.ApiKey);
        request.Headers.Add("API-Sign", signature);

        HttpResponseMessage response = await HttpClient.SendAsync(request);
        string responseJson = await response.Content.ReadAsStringAsync();
        Debug.WriteLine($"Response: {response.StatusCode}");
        using JsonDocument doc = JsonDocument.Parse(responseJson);
        JsonElement resultElement = doc.RootElement.GetProperty("result");

        Dictionary<string, decimal> dict = [];
        foreach (JsonProperty property in resultElement.EnumerateObject())
        {
            if (decimal.TryParse(property.Value.GetString(), out decimal value))
            {
                dict[property.Name] = value;
            }
        }

        List<KrakenPosition> positions = [];

        foreach (KeyValuePair<string, decimal> pair in dict)
        {
            if (pair.Value == 0)
                continue;
            positions.Add(new KrakenPosition
            {
                Currency = pair.Key,
                Quantity = Convert.ToDouble(pair.Value)
            });
        }

        Cache.LastCallContent = positions;
        return positions;
    }

    private static class Cache
    {
        public static List<KrakenPosition>? LastCallContent { get; set; } = null; // limit to one call, crypto unlikely to change frequently
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nova.APIs;
public class Kraken
{
    private static readonly string apiKey = Environment.GetEnvironmentVariable("NOVA-KrakenAPIKey")!;
    private static readonly string apiSecret = Environment.GetEnvironmentVariable("NOVA-KrakenAPISecret")!;

    public static string GetKrakenSignature(string urlPath, string nonce, string postData, string apiSecret)
    {
        // Step 1: SHA256(nonce + postData)
        byte[] npBytes = Encoding.UTF8.GetBytes(nonce + postData);
        using SHA256 sha256 = SHA256.Create();
        byte[] hash256 = sha256.ComputeHash(npBytes);

        // Step 2: Concatenate urlPath + hash256
        byte[] pathBytes = Encoding.UTF8.GetBytes(urlPath);
        byte[] buffer = new byte[pathBytes.Length + hash256.Length];
        Buffer.BlockCopy(pathBytes, 0, buffer, 0, pathBytes.Length);
        Buffer.BlockCopy(hash256, 0, buffer, pathBytes.Length, hash256.Length);

        // Step 3: HMAC-SHA512 with base64-decoded secret
        byte[] secretBytes = Convert.FromBase64String(apiSecret);
        using HMACSHA512 hmac = new HMACSHA512(secretBytes);
        byte[] hash512 = hmac.ComputeHash(buffer);

        // Step 4: Return base64-encoded signature
        return Convert.ToBase64String(hash512);
    }

    static HttpClient HttpClient { get; set; } = new HttpClient();
    public static async Task<List<KrakenPosition>> GetBalanceAsync()
    {
        if (Cache.LastCallContent != null)
        {
            return Cache.LastCallContent;
        }

        string urlPath = "/0/private/Balance";
        string apiUrl = "https://api.kraken.com" + urlPath;

        string nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        string postData = $"nonce={nonce}";

        // Generate API-Sign
        string signature = GetKrakenSignature(urlPath, nonce, postData, apiSecret);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded")
        };
        request.Headers.Add("API-Key", apiKey);
        request.Headers.Add("API-Sign", signature);

        HttpResponseMessage response = await HttpClient.SendAsync(request);
        string responseJson = await response.Content.ReadAsStringAsync();
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
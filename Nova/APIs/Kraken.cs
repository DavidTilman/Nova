using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Nova.APIs;
internal class Kraken
{
    static HttpClient HttpClient { get; set; } = new HttpClient();

    public static async Task GetBalanceAsync()
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

        return responseJson;
    }
}

namespace Nova.APIs;
public class Trading212Position
{
    [System.Text.Json.Serialization.JsonPropertyName("ticker")]
    public string? Ticker { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("quantity")]
    public double Quantity { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("averagePrice")]
    public double AveragePrice { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("currentPrice")]
    public double CurrentPrice { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("ppl")]
    public double Gain { get; set; }

    public string FormattedGain => this.Gain < 0 ? $"▼ {this.Gain:C2}" : $"▲ {this.Gain:C2}";

    public string Name => Trading212.InstrumentData
        .FirstOrDefault(x => x.ContainsKey("ticker") && x["ticker"].GetString() == this.Ticker)?["name"].GetString() ?? "Unknown";

    public string ISIN => Trading212.InstrumentData
        .FirstOrDefault(x => x.ContainsKey("ticker") && x["ticker"].GetString() == this.Ticker)?["isin"].GetString() ?? "Unknown";
}
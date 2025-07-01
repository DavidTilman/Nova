using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

}

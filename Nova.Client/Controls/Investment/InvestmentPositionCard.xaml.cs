using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Nova.APIs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Controls;
public sealed partial class InvestmentPositionCard : UserControl
{
    public Trading212Position? Position { get; private set; }
    public string Ticker => (Position?.Ticker) ?? "Symbol";
    public string Quantity => (Position?.Quantity.ToString()) ?? "Owned Shares";
    public string AveragePrice => ((Position?.AveragePrice / (((Position?.CurrentPrice - Position?.AveragePrice) * Position?.Quantity) / Position?.Gain))?.ToString("C")) ?? "Cur.Adj. Average Price";
    public string CurrentPrice => ((Position?.CurrentPrice / (((Position?.CurrentPrice - Position?.AveragePrice) * Position?.Quantity) / Position?.Gain))?.ToString("C")) ?? "Cur.Adj. Current Price";
    public string Gain => (Position?.Gain.ToString("C")) ?? "Gain";
    public InvestmentPositionCard(Trading212Position position)
    {
        Position = position;
        this.InitializeComponent();
    }

    public InvestmentPositionCard()
    {
        Position = null;
        this.InitializeComponent();
    }
}

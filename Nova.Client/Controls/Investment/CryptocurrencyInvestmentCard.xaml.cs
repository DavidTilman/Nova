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

namespace Nova.Client.Controls.Investment;
public sealed partial class CryptocurrencyInvestmentCard : UserControl
{
    public KrakenPosition? Position { get; private set; }
    public string Currency => (Position?.Currency) ?? "Currency";
    public string Quantity => (Position?.Quantity.ToString()) ?? "Owned";
    public CryptocurrencyInvestmentCard(KrakenPosition position)
    {
        Position = position;
        this.InitializeComponent();
    }

    public CryptocurrencyInvestmentCard()
    {
        Position = null;
        this.InitializeComponent();
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Nova.APIs;
using Nova.Client.Controls;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Nova.Client;
public sealed partial class MainPage : Page
{
    public List<KrakenPosition> CryptoPositions
    {
        set => this.InvestmentPanel.CryptoPositions = value;
    }

    public List<Trading212Position> InvestmentPositions
    {
        set => this.InvestmentPanel.InvestmentPositions = value;
    }

    public List<Account> Accounts
    {
        set => this.WealthDistributionPanel.Accounts = value;
    }

    public MainPage()
    {
        this.InitializeComponent();
    }
}
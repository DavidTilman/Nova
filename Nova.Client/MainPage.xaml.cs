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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page
{
    public List<KrakenPosition> CryptoPositions
    {
        set
        {
            InvestmentPanel.CryptoPositions = value;
        }
    }

    public List<Trading212Position> InvestmentPositions
    {
        set
        {
            InvestmentPanel.InvestmentPositions = value;
        }
    }

    public List<Account> Accounts
    {
        set
        {
            WealthDistributionPanel.Accounts = value;
        }
    }

    public MainPage()
    {
        this.InitializeComponent();
    }
}
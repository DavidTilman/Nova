using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Nova.Database;
using Nova.APIs;
using System.Diagnostics;

namespace Nova.Client.Controls;

public sealed partial class SidePanel : UserControl
{
    public event EventHandler? OpenPaymentForm;
    public List<Account> Accounts
    {
        set
        {
            foreach (Account account in value)
            {
                AccountTabCompact tab = new AccountTabCompact(account);
                this.AccountsStackPanel.Children.Add(tab);
            }
        }
    }

    public List<Trading212Position> InvestmentPositions
    {
        set
        {
            foreach (Trading212Position position in value)
            {
                InvestmentTabCompact tab = new InvestmentTabCompact(position);
                this.InvestmentStackPanel.Children.Add(tab);
            }
        }
    }
    public List<KrakenPosition> CryptoPositions
    {
        set
        {
            foreach (KrakenPosition position in value)
            {
                CrytpoPresenter tab = new CrytpoPresenter(position);
                this.CryptoStackPanel.Children.Add(tab);
            }
        }
    }
    public SidePanel()
    {
        this.InitializeComponent();

        this.Loaded += this.SidePanel_Loaded;
    }

    private async void SidePanel_Loaded(object sender, RoutedEventArgs e)
    {
        Dictionary<char, double> timeChanges = await AccountManager.GetTimeChangesAsync();

        TimeChangeTab wTimeChangeTab;
        TimeChangeTab mTimeChangeTab;
        TimeChangeTab qTimeChangeTab;
        TimeChangeTab yTimeChangeTab;

        wTimeChangeTab = new TimeChangeTab("W", timeChanges['w']);
        mTimeChangeTab = new TimeChangeTab("M", timeChanges['m']);
        qTimeChangeTab = new TimeChangeTab("Q", timeChanges['q']);
        yTimeChangeTab = new TimeChangeTab("Y", timeChanges['y']);

        wTimeChangeTab.VerticalAlignment = VerticalAlignment.Center;
        mTimeChangeTab.VerticalAlignment = VerticalAlignment.Center;
        qTimeChangeTab.VerticalAlignment = VerticalAlignment.Center;
        yTimeChangeTab.VerticalAlignment = VerticalAlignment.Center;

        wTimeChangeTab.HorizontalAlignment = HorizontalAlignment.Center;
        mTimeChangeTab.HorizontalAlignment = HorizontalAlignment.Center;
        qTimeChangeTab.HorizontalAlignment = HorizontalAlignment.Center;
        yTimeChangeTab.HorizontalAlignment = HorizontalAlignment.Center;

        Grid.SetColumn(wTimeChangeTab, 0);
        Grid.SetRow(wTimeChangeTab, 0);

        Grid.SetColumn(mTimeChangeTab, 1);
        Grid.SetRow(mTimeChangeTab, 0);

        Grid.SetColumn(qTimeChangeTab, 0);
        Grid.SetRow(qTimeChangeTab, 1);

        Grid.SetColumn(yTimeChangeTab, 1);
        Grid.SetRow(yTimeChangeTab, 1);

        TimeChangeGrid.Children.Add(wTimeChangeTab);
        TimeChangeGrid.Children.Add(mTimeChangeTab);
        TimeChangeGrid.Children.Add(qTimeChangeTab);
        TimeChangeGrid.Children.Add(yTimeChangeTab);
    }

    private void PaymentButton_Click(object sender, RoutedEventArgs e)
    {
        OpenPaymentForm?.Invoke(this, EventArgs.Empty);
    }
}
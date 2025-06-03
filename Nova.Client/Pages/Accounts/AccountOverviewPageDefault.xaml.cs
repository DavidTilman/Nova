using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Nova.Database;

using Syncfusion.UI.Xaml.Charts;

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Pages.Accounts;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AccountOverviewPageDefault : Page
{
    public string FormattedNetWorth => AccountManager.FormattedNetWorth;
    public AccountOverviewPageDefault()
    {
        this.InitializeComponent();
        List<AccountEvent> events = AccountManager.GetAllAccountEvents();
        foreach (AccountEvent accountEvent in events)
        {
            AccountEventsListView.Items.Add(
                new Controls.AccountEventCardUserControl(accountEvent, true));
        }
        NumberAccountsTextBlock.Text = AccountManager.GetAccounts().Count.ToString();
        AllTimeIncomeTextBlock.Text = ((from AccountEvent e in events where e.EventType == AccountEventType.Income select e.Value).Sum() ?? 0).ToString("C");
        AllTimeSpendingTextBlock.Text = ((from AccountEvent e in events where e.EventType == AccountEventType.Payment select e.Value).Sum() ?? 0).ToString("C");

        Dictionary<char, double> timeChangeNetWorth = Database.AccountManager.GetTimeChanges();

        WeekChangeIndicatorTextBlock.Text = timeChangeNetWorth['w'].ToString("C");
        MonthChangeIndicatorTextBlock.Text = timeChangeNetWorth['m'].ToString("C");
        QuarterChangeIndicatorTextBlock.Text = timeChangeNetWorth['q'].ToString("C");
        YearChangeIndicatorTextBlock.Text = timeChangeNetWorth['y'].ToString("C");

        if (timeChangeNetWorth['w'] >= 0)
        {
            WeekChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
        }
        else
        {
            WeekChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
        }
        if (timeChangeNetWorth['m'] >= 0)
        {
            MonthChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
        }
        else
        {
            MonthChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
        }
        if (timeChangeNetWorth['q'] >= 0)
        {
            QuarterChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
        }
        else
        {
            QuarterChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
        }
        if (timeChangeNetWorth['y'] >= 0)
        {
            YearChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
        }
        else
        {
            YearChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
        }


        DateTimeAxis xAxis = new DateTimeAxis();
        xAxis.ShowMajorGridLines = false;
        NetWorthChart.XAxes.Add(xAxis);

        NumericalAxis yAxis = new NumericalAxis();
        NetWorthChart.YAxes.Add(yAxis);

        LineSeries series = new LineSeries();
        series.ItemsSource = events;
        series.XBindingPath = "TimeStamp";
        series.YBindingPath = "NetWorth";
        series.Fill = (Application.Current.Resources["AccentTextFillColorTertiaryBrush"] as SolidColorBrush);
        //Adding Series to the Chart Series Collection
        NetWorthChart.Series.Add(series);
    }
}

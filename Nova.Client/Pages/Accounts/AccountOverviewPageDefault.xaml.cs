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

        NumberAccountsTextBlock.Text = AccountManager.GetAccounts().Count.ToString();
        
        this.DispatcherQueue.TryEnqueue(async () =>
        {
            List<AccountEvent> events = await AccountManager.GetAllAccountEventsAsync();

            foreach (AccountEvent accountEvent in events)
            {
                AccountEventsListView.Items.Add(
                    new Controls.AccountEventCardUserControl(accountEvent, true));
            }

            AllTimeIncomeTextBlock.Text = ((from AccountEvent e in events where e.EventType == AccountEventType.Income select e.Value).Sum() ?? 0).ToString("C");
            AllTimeSpendingTextBlock.Text = ((from AccountEvent e in events where e.EventType == AccountEventType.Payment select e.Value).Sum() ?? 0).ToString("C");

            DateTimeAxis xAxis = new()
            {
                ShowMajorGridLines = false
            };
            NetWorthChart.XAxes.Add(xAxis);

            NumericalAxis yAxis = new();
            NetWorthChart.YAxes.Add(yAxis);

            LineSeries series = new()
            {
                ItemsSource = events,
                XBindingPath = "TimeStamp",
                YBindingPath = "NetWorth",
                Fill = Application.Current.Resources["AccentTextFillColorTertiaryBrush"] as SolidColorBrush
            };
            //Adding Series to the Chart Series Collection
            NetWorthChart.Series.Add(series);
        });

        this.DispatcherQueue.TryEnqueue(async () =>
        {
            Dictionary<char, double> timeChangeNetWorth = await Database.AccountManager.GetTimeChangesAsync();

            WeekChangeIndicatorTextBlock.Text = timeChangeNetWorth['w'].ToString("C");
            MonthChangeIndicatorTextBlock.Text = timeChangeNetWorth['m'].ToString("C");
            QuarterChangeIndicatorTextBlock.Text = timeChangeNetWorth['q'].ToString("C");
            YearChangeIndicatorTextBlock.Text = timeChangeNetWorth['y'].ToString("C");

            WeekChangeIndicatorTextBlock.Foreground = timeChangeNetWorth['w'] >= 0
                ?  (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"]
                : (Brush) (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
            
            MonthChangeIndicatorTextBlock.Foreground = timeChangeNetWorth['m'] >= 0
                ?  (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"]
                : (Brush) (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
            
            QuarterChangeIndicatorTextBlock.Foreground = timeChangeNetWorth['q'] >= 0
                ?  (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"]
                : (Brush) (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
            
            YearChangeIndicatorTextBlock.Foreground = timeChangeNetWorth['y'] >= 0
                ?  (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"]
                : (Brush) (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
        });
    }
}

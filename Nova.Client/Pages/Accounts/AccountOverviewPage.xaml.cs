using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using Syncfusion.UI.Xaml.Charts;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
public sealed partial class AccountOverviewPage : Page
{
    public Nova.Account? Account = null;
    public List<AccountEvent> AccountEvents= [];

    public AccountOverviewPage()
    {
        this.InitializeComponent();

    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Nova.Account account)
            return;

        Account = account;
        AccountEvents = Nova.Database.AccountManager.GetAccountEvents(account);

        if (account.AccountType != AccountType.Current)
        {
            AddIncomeButton.Visibility = Visibility.Collapsed;
            MakePaymentButton.Visibility = Visibility.Collapsed;
        }
        if (account.AccountType != AccountType.Investment)
        {
            UpdateValueButton.Visibility = Visibility.Collapsed;
        }

        foreach (AccountEvent accountEvent in AccountEvents)
        {
            Debug.WriteLine($"Adding event: {accountEvent.EventType} with value {accountEvent.Value}");
            AccountEventsListView.Items.Add(
                new Controls.AccountEventCardUserControl(accountEvent));
        }

        Dictionary<char, double> timeChanges =
            Nova.Database.AccountManager.GetTimeChanges(account);

        WeekChangeIndicatorTextBlock.Text = timeChanges['w'].ToString("C");
        MonthChangeIndicatorTextBlock.Text = timeChanges['m'].ToString("C");
        QuarterChangeIndicatorTextBlock.Text = timeChanges['q'].ToString("C");
        YearChangeIndicatorTextBlock.Text = timeChanges['y'].ToString("C");

        if (timeChanges['w'] < 0)
        {
            WeekChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
        }
        else if (timeChanges['w'] > 0)
        {
            WeekChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
        }

        if (timeChanges['m'] < 0)
        {
            MonthChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
        }
        else if (timeChanges['m'] > 0)
        {
            MonthChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
        }

        if (timeChanges['q'] < 0)
        {
            QuarterChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
        }
        else if (timeChanges['q'] > 0)
        {
            QuarterChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
        }

        if (timeChanges['y'] < 0)
        {
            YearChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
        }
        else if (timeChanges['y'] > 0)
        {
            YearChangeIndicatorTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
        }

        if (Account.AccountType != AccountType.Current)
        {
            AllTimeIncomeLabelTextBlock.Visibility = Visibility.Collapsed;
            AllTimeIncomeTextBlock.Visibility = Visibility.Collapsed;

            AllTimeSpendingLabelTextBlock.Visibility = Visibility.Collapsed;
            AllTimeSpendingTextBlock.Visibility = Visibility.Collapsed;

            HighestPayeeLabelTextBlock.Visibility = Visibility.Collapsed;
            HighestPayeeTextBlock.Visibility = Visibility.Collapsed;
        }


        double? allTimeIncome = (from AccountEvent accountEvent in AccountEvents where accountEvent.EventType == AccountEventType.Income select accountEvent.Value.HasValue ? accountEvent.Value : 0).Sum();
        if (allTimeIncome.HasValue)
        {
            AllTimeIncomeTextBlock.Text = allTimeIncome.Value.ToString("C");
        }

        double? allTimeSpending = (from AccountEvent accountEvent in AccountEvents where accountEvent.EventType == AccountEventType.Payment select accountEvent.Value.HasValue? accountEvent.Value : 0).Sum();
        if (allTimeSpending.HasValue) 
        {
            AllTimeSpendingTextBlock.Text = allTimeSpending.Value.ToString("C");
        }

        double? allTimeInterest= (from AccountEvent accountEvent in AccountEvents where accountEvent.EventType == AccountEventType.Interest select accountEvent.Value.HasValue ? accountEvent.Value : 0).Sum();
        if (allTimeInterest.HasValue)
        {
            AllTimeInterestTextBlock.Text = allTimeInterest.Value.ToString("C");
        }

        double? netTransfers = (from AccountEvent accountEvent in AccountEvents where accountEvent.EventType == AccountEventType.Transfer select accountEvent.Value.HasValue ? accountEvent.Value : 0).Sum();
        if (netTransfers.HasValue)
        {
            NetTransfersTextBlock.Text = netTransfers.Value.ToString("C");
            if (netTransfers.Value < 0)
            {
                NetTransfersTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
            }
            else if (netTransfers.Value > 0)
            {
                NetTransfersTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
            }
        }

        string highestpayee = Nova.Database.AccountManager.GetHighestPayee(account, out double amount);
        string payeeText = $"{highestpayee} ({amount.ToString("C")})";

        HighestPayeeTextBlock.Text = payeeText;

        DateTimeAxis xAxis = new DateTimeAxis();
        xAxis.ShowMajorGridLines = false;
        BalanceChart.XAxes.Add(xAxis);

        NumericalAxis yAxis = new NumericalAxis();
        BalanceChart.YAxes.Add(yAxis);

        LineSeries series = new LineSeries();
        series.ItemsSource = AccountEvents;
        series.XBindingPath = "TimeStamp";
        series.YBindingPath = "NewBalance";
        series.Fill = (Application.Current.Resources["AccentTextFillColorTertiaryBrush"] as SolidColorBrush);
        //Adding Series to the Chart Series Collection
        BalanceChart.Series.Add(series);
    }

    private async void AddIncomeButton_Click(object sender, RoutedEventArgs e)
    {
        AddIncomeForm incomeForm = new AddIncomeForm();
        ContentDialog incomeFormDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Title = "Add Income",
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            Content = incomeForm
        };
        ContentDialogResult result = await incomeFormDialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        if (incomeForm.IncomeValue <= 0)
            return;

        try
        {
            Nova.Database.AccountManager.AddIncome(Account, incomeForm.IncomeValue, incomeForm.IncomeSource);
        }
        catch (Exception ex)
        {
            ContentDialog errorDialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title = "Error",
                Content = $"{ex.Message}",
                CloseButtonText = "Ok"
            };
            await errorDialog.ShowAsync();
            return;
        }
    }

    private async void TransferFundsButton_Click(object sender, RoutedEventArgs e)
    {
        TransferFundsForm transferForm = new TransferFundsForm(Account);
        ContentDialog transferFormDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Title = "Transfer Funds",
            PrimaryButtonText = "Transfer",
            CloseButtonText = "Cancel",
            Content = transferForm
        };
        ContentDialogResult result = await transferFormDialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        if (transferForm.TransferAmount <= 0)
            return;

        if (transferForm.ToAccountID == -1)
            return;

        Account toAccount = Nova.Database.AccountManager.GetAccount(transferForm.ToAccountID);
        try
        {
            Nova.Database.AccountManager.MakeTransfer(toAccount, Account, transferForm.TransferAmount);
        }
        catch (Exception ex)
        {
            ContentDialog errorDialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title = "Error",
                Content = $"{ex.Message}",
                CloseButtonText = "Ok"
            };
            await errorDialog.ShowAsync();
            return;
        }
    }

    private async void MakePaymentButton_Click(object sender, RoutedEventArgs e)
    {
        MakePaymentForm paymentForm = new MakePaymentForm(Account);
        ContentDialog paymentFormDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Title = "Make Payment",
            PrimaryButtonText = "Pay",
            CloseButtonText = "Cancel",
            Content = paymentForm
        };
        ContentDialogResult result = await paymentFormDialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        if (paymentForm.PaymentAmount <= 0)
            return;

        try
        {
            Nova.Database.AccountManager.MakePayment(Account,
                paymentForm.PaymentAmount, paymentForm.PaymentPayee);
        }
        catch (Exception ex)
        {
            ContentDialog errorDialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title = "Error",
                Content = $"{ex.Message}",
                CloseButtonText = "Ok"
            };
            await errorDialog.ShowAsync();
            return;
        }
    }

    private async void AddInterestButton_Click(object sender, RoutedEventArgs e)
    {
        AddInterestForm interestForm = new AddInterestForm();
        ContentDialog interestFormDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Title = "Add Interest",
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            Content = interestForm
        };
        ContentDialogResult result = await interestFormDialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        if (interestForm.InterestValue <= 0)
            return;

        try
        {
            Nova.Database.AccountManager.AddInterest(Account,
                interestForm.InterestValue);
        }
        catch (Exception ex)
        {
            ContentDialog errorDialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title = "Error",
                Content = $"{ex.Message}",
                CloseButtonText = "Ok"
            };
            await errorDialog.ShowAsync();
            return;
        }
    }

    private async void UpdateValueButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateValueForm updateForm = new UpdateValueForm();
        ContentDialog updateFormDialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Title = "Update value",
            PrimaryButtonText = "Update",
            CloseButtonText = "Cancel",
            Content = updateForm
        };
        ContentDialogResult result = await updateFormDialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        if (updateForm.NewBalance <= 0)
            return;

        try
        {
            Nova.Database.AccountManager.UpdateValue(Account,
                updateForm.NewBalance);
        }
        catch (Exception ex)
        {
            ContentDialog errorDialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title = "Error",
                Content = $"{ex.Message}",
                CloseButtonText = "Ok"
            };
            await errorDialog.ShowAsync();
            return;
        }
    }
}

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

using WinRT;

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

    public AccountOverviewPage() => this.InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not Nova.Account account)
            return;

        Account = account;
        AccountEvents = await Nova.Database.AccountManager.GetAccountEventsAsync(account);

        this.DispatcherQueue.TryEnqueue(() =>
        {
            double? allTimeIncome = (from AccountEvent accountEvent in AccountEvents where accountEvent.EventType == AccountEventType.Income select accountEvent.Value.HasValue ? accountEvent.Value : 0).Sum();
            if (allTimeIncome.HasValue)
            {
                AllTimeIncomeTextBlock.Text = allTimeIncome.Value.ToString("C");
            }

            double? allTimeSpending = (from AccountEvent accountEvent in AccountEvents where accountEvent.EventType == AccountEventType.Payment select accountEvent.Value.HasValue ? accountEvent.Value : 0).Sum();
            if (allTimeSpending.HasValue)
            {
                AllTimeSpendingTextBlock.Text = allTimeSpending.Value.ToString("C");
            }

            double? allTimeInterest = (from AccountEvent accountEvent in AccountEvents where accountEvent.EventType == AccountEventType.Interest select accountEvent.Value.HasValue ? accountEvent.Value : 0).Sum();
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

            foreach (AccountEvent accountEvent in AccountEvents)
            {
                AccountEventsListView.Items.Add(
                    new Controls.AccountEventCardUserControl(accountEvent));
            }

            DateTimeAxis xAxis = new()
            {
                ShowMajorGridLines = false
            };
            BalanceChart.XAxes.Add(xAxis);

            NumericalAxis yAxis = new();
            BalanceChart.YAxes.Add(yAxis);

            StepLineSeries series = new()
            {
                ItemsSource = AccountEvents,
                XBindingPath = "TimeStamp",
                YBindingPath = "NewBalance",
                Fill = Application.Current.Resources["AccentTextFillColorTertiaryBrush"] as SolidColorBrush
            };
            //Adding Series to the Chart Series Collection
            BalanceChart.Series.Add(series);
        });

        this.DispatcherQueue.TryEnqueue(async () =>
        {
            Dictionary<char, double> timeChanges = await Nova.Database.AccountManager.GetTimeChangesAsync(account);

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
        });

        this.DispatcherQueue.TryEnqueue(async () =>
        {
            (string payee, double amount) highestPayee = await Nova.Database.AccountManager.GetHighestPayeeAsync(account);
            string payeeText = $"{highestPayee.payee} ({highestPayee.amount:C})";

            HighestPayeeTextBlock.Text = payeeText;
        });

        if (account.AccountType != AccountType.Current)
        {
            AddIncomeButton.Visibility = Visibility.Collapsed;
            MakePaymentButton.Visibility = Visibility.Collapsed;

            AllTimeIncomeLabelTextBlock.Visibility = Visibility.Collapsed;
            AllTimeIncomeTextBlock.Visibility = Visibility.Collapsed;

            AllTimeSpendingLabelTextBlock.Visibility = Visibility.Collapsed;
            AllTimeSpendingTextBlock.Visibility = Visibility.Collapsed;

            HighestPayeeLabelTextBlock.Visibility = Visibility.Collapsed;
            HighestPayeeTextBlock.Visibility = Visibility.Collapsed;
        }
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
            Nova.Database.AccountManager.AddIncomeAsync(Account, incomeForm.IncomeValue, incomeForm.IncomeSource);
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

        Account toAccount = Nova.Database.AccountManager.GetAccountAsync(transferForm.ToAccountID).Result;
        try
        {
            Nova.Database.AccountManager.MakeTransferAsync(toAccount, Account, transferForm.TransferAmount);
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
            Nova.Database.AccountManager.MakePaymentAsync(Account,
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
            Nova.Database.AccountManager.AddInterestAsync(Account,
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
            Nova.Database.AccountManager.UpdateValueAsync(Account,
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

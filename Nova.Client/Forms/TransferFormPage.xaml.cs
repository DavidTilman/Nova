using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Nova.Database;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Nova.Client.Forms;
public sealed partial class TransferFormPage : Page
{
    MainWindow? MainWindow;

    List<string> AccountStrings = [];

    public TransferFormPage()
    {
        this.InitializeComponent();
        this.Loaded += this.TransferFormPage_Loaded;
    }

    private async void TransferFormPage_Loaded(object sender, RoutedEventArgs e)
    {
        foreach (Account account in await AccountManager.GetAccountsAsync())
        {
            if (account.AccountType is not AccountType.Current)
                continue;
            AccountStrings.Add($"[{account.ID}] {account.AccountName} ({account.AccountProvider})");
        }

        FromAccountCombobox.ItemsSource = AccountStrings;

        TransferDatePicker.Date = DateTimeOffset.UtcNow;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is MainWindow window)
        {
            MainWindow = window;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        MainWindow!.CloseOverlay();
    }

    private void FromAccountCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string selectedFromAccount = FromAccountCombobox.SelectedItem as string ?? string.Empty;
        foreach (string accountString in AccountStrings)
        {
            if (accountString == selectedFromAccount)
                continue;
            ToAccountCombobox.Items.Add(accountString);
        }

        ToAccountCombobox.IsEnabled = true;
    }

    private void ToAccountCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    private async void SubmitTransferButton_Click(object sender, RoutedEventArgs e)
    {
        int? toSelectedAccountID = FormHelper.ExtractID((ToAccountCombobox.SelectedItem as string)!);

        if (toSelectedAccountID is null)
            return;

        int? fromSelectedAccountID = FormHelper.ExtractID((FromAccountCombobox.SelectedItem as string)!);

        if (fromSelectedAccountID is null)
            return;

        Account? toAccount = await AccountManager.GetAccountAsync(toSelectedAccountID.Value);
        if (toAccount is null)
            return;

        Account? fromAccount = await AccountManager.GetAccountAsync(fromSelectedAccountID.Value);
        if (fromAccount is null)
            return;

        double amount = double.Parse(AmountNumberBox.Text);

        int dayOffset = (int) Math.Ceiling((TransferDatePicker.Date - DateTimeOffset.UtcNow).TotalDays);
        DateTime timeStamp = DateTime.UtcNow.AddDays(dayOffset);

        MainWindow!.CloseOverlay();
        await AccountManager.MakeTransferAsync(toAccount, fromAccount, amount, timeStamp);
    }
}
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;

using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Nova.Client.Forms;
public sealed partial class PaymentFormPage : Page
{
    List<string> payeeSuggestions = [];

    MainWindow? MainWindow;
    public PaymentFormPage()
    {
        this.InitializeComponent();

        this.Loaded += this.PaymentFormPage_Loaded;
    }

    private async void PaymentFormPage_Loaded(object sender, RoutedEventArgs e)
    {
        foreach (Account account in await AccountManager.GetAccountsAsync())
        {
            if (account.AccountType is not AccountType.Current)
                continue;
            AccountsCombobox.Items.Add($"[{account.ID}] {account.AccountName} ({account.AccountProvider})");
        }

        PaymentDatePicker.Date = DateTimeOffset.UtcNow;
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

    private async void AccountsCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AccountsCombobox.SelectedItem is null)
            payeeSuggestions = [];

        int? selectedAccountID = FormHelper.ExtractID((AccountsCombobox.SelectedItem as string)!);

        if (selectedAccountID is null)
            return;

        payeeSuggestions = await AccountManager.GetPayeesAsync(selectedAccountID.Value);
    }

    private void PayeeAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            List<string> suitableItems = [];
            string[] splitText = sender.Text.ToLower().Split(" ");
            foreach (string payee in payeeSuggestions)
            {
                bool found = splitText.All((key) => payee.Contains(key, StringComparison.CurrentCultureIgnoreCase));

                if (found)
                {
                    suitableItems.Add(payee);
                }
            }

            sender.ItemsSource = suitableItems;
        }
    }

    private void PayeeAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        PayeeAutoSuggestBox.Text = args.SelectedItem.ToString();
    }

    private async void SubmitPaymentButton_Click(object sender, RoutedEventArgs e)
    {
        int? selectedAccountID = FormHelper.ExtractID((AccountsCombobox.SelectedItem as string)!);

        if (selectedAccountID is null)
            return;

        Account? account = await AccountManager.GetAccountAsync(selectedAccountID.Value);
        if (account is null)
            return;

        double amount = double.Parse(AmountNumberBox.Text);

        string payee = PayeeAutoSuggestBox.Text;

        int dayOffset = (int) Math.Ceiling((PaymentDatePicker.Date - DateTimeOffset.UtcNow).TotalDays);
        DateTime timeStamp = DateTime.UtcNow.AddDays(dayOffset);

        MainWindow!.CloseOverlay();
        await AccountManager.MakePaymentAsync(account, amount, payee, timeStamp);

    }
}
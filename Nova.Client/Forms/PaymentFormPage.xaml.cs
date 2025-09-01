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

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Forms;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
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

    public static int? ExtractID(string formattedAccountString)
    {
        if (string.IsNullOrWhiteSpace(formattedAccountString))
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(formattedAccountString, @"\[(\d+)\]");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }


    private async void AccountsCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AccountsCombobox.SelectedItem is null)
            payeeSuggestions = [];

        int? selectedAccountID = ExtractID((AccountsCombobox.SelectedItem as string)!);

        if (selectedAccountID is null)
            return;

        payeeSuggestions = await AccountManager.GetPayeesAsync(selectedAccountID.Value);
    }

    private void PayeeAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            List<string> suitableItems = new List<string>();
            string[] splitText = sender.Text.ToLower().Split(" ");
            foreach (string payee in payeeSuggestions)
            {
                bool found = splitText.All((key) =>
                {
                    return payee.ToLower().Contains(key);
                });

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
        int? selectedAccountID = ExtractID((AccountsCombobox.SelectedItem as string)!);

        if (selectedAccountID is null)
            return;

        Account? account = await AccountManager.GetAccountAsync(selectedAccountID.Value);
        if (account is null)
            return;

        double amount = double.Parse(AmountNumberBox.Text);

        string payee = PayeeAutoSuggestBox.Text;

        int dayOffset = (int) Math.Ceiling((PaymentDatePicker.Date - DateTimeOffset.UtcNow).TotalDays);
        DateTime timeStamp = DateTime.UtcNow.AddDays(dayOffset);

        await AccountManager.MakePaymentAsync(account, amount, payee, timeStamp);
    }
}
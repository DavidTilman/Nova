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
using System.Text.RegularExpressions;

using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Nova.Client.Forms;
public sealed partial class IncomeFormPage : Page
{
    MainWindow? MainWindow;

    List<string> sourceSuggestions = [];

    public IncomeFormPage()
    {
        this.InitializeComponent();
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

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        foreach (Account account in await AccountManager.GetAccountsAsync())
        {
            if (account.AccountType is not AccountType.Current)
                continue;
            AccountsCombobox.Items.Add($"[{account.ID}] {account.AccountName} ({account.AccountProvider})");
        }

        IncomeDatePicker.Date = DateTimeOffset.UtcNow;
    }

    private async void AccountsCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AccountsCombobox.SelectedItem is null)
            sourceSuggestions = [];

        int? selectedAccountID = ExtractID((AccountsCombobox.SelectedItem as string)!);

        if (selectedAccountID is null)
            return;

        sourceSuggestions = await AccountManager.GetIncomeSourcesAsync(selectedAccountID.Value);
    }

    public static int? ExtractID(string formattedAccountString)
    {
        if (string.IsNullOrWhiteSpace(formattedAccountString))
            return null;

        Match match = NumberContainedInSquareBrackets().Match(formattedAccountString);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    [GeneratedRegex(@"\[(\d+)\]")]
    private static partial Regex NumberContainedInSquareBrackets();

    private void SourceAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            List<string> suitableItems = [];
            string[] splitText = sender.Text.ToLower().Split(" ");
            foreach (string source in sourceSuggestions)
            {
                bool found = splitText.All((key) => source.Contains(key, StringComparison.CurrentCultureIgnoreCase));

                if (found)
                {
                    suitableItems.Add(source);
                }
            }

            sender.ItemsSource = suitableItems;
        }
    }

    private void SourceAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        SourceAutoSuggestBox.Text = args.SelectedItem.ToString();
    }

    private async void LogIncomeButton_Click(object sender, RoutedEventArgs e)
    {
        int? selectedAccountID = ExtractID((AccountsCombobox.SelectedItem as string)!);

        if (selectedAccountID is null)
            return;

        Account? account = await AccountManager.GetAccountAsync(selectedAccountID.Value);
        if (account is null)
            return;

        double amount = double.Parse(AmountNumberBox.Text);

        string payee = SourceAutoSuggestBox.Text;

        int dayOffset = (int) Math.Ceiling((IncomeDatePicker.Date - DateTimeOffset.UtcNow).TotalDays);
        DateTime timeStamp = DateTime.UtcNow.AddDays(dayOffset);

        await AccountManager.AddIncomeAsync(account, amount, payee, timeStamp);

        MainWindow!.CloseOverlay();
    }
}


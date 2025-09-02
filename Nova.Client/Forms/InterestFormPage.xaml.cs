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
public sealed partial class InterestFormPage : Page
{
    MainWindow? MainWindow;
    public InterestFormPage()
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

    private async void LogInterestButton_Click(object sender, RoutedEventArgs e)
    {
        int? selectedAccountID = FormHelper.ExtractID((AccountsCombobox.SelectedItem as string)!);

        if (selectedAccountID is null)
            return;

        Account? account = await AccountManager.GetAccountAsync(selectedAccountID.Value);
        if (account is null)
            return;

        double amount = double.Parse(AmountNumberBox.Text);

        int dayOffset = (int) Math.Ceiling((InterestDatePicker.Date - DateTimeOffset.UtcNow).TotalDays);
        DateTime timeStamp = DateTime.UtcNow.AddDays(dayOffset);

        MainWindow!.CloseOverlay();

        await AccountManager.AddInterestAsync(account, amount, timeStamp);
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        foreach (Account account in await AccountManager.GetAccountsAsync())
        {
            AccountsCombobox.Items.Add($"[{account.ID}] {account.AccountName} ({account.AccountProvider})");
        }

        InterestDatePicker.Date = DateTimeOffset.UtcNow;
    }
}

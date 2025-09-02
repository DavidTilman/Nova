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
public sealed partial class UpdateFormPage : Page
{
    MainWindow? MainWindow;
    public UpdateFormPage()
    {
        this.InitializeComponent();

        this.Loaded += this.UpdateFormPage_Loaded;
    }

    private async void UpdateFormPage_Loaded(object sender, RoutedEventArgs e)
    {
        foreach (Account account in await AccountManager.GetAccountsAsync())
        {
            if (account.AccountType is not AccountType.Investment and not AccountType.Asset)
                continue;
            AccountsCombobox.Items.Add($"[{account.ID}] {account.AccountName} ({account.AccountProvider})");
        }

        UpdateDatePicker.Date = DateTimeOffset.UtcNow;
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

    private async void SubmitUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        int? selectedAccountID = FormHelper.ExtractID((AccountsCombobox.SelectedItem as string)!);

        if (selectedAccountID is null)
            return;

        Account? account = await AccountManager.GetAccountAsync(selectedAccountID.Value);
        if (account is null)
            return;

        if (!double.TryParse(AmountNumberBox.Text, out double amount))
        {
            return;
        }

        int dayOffset = (int) Math.Ceiling((UpdateDatePicker.Date - DateTimeOffset.UtcNow).TotalDays);
        DateTime timeStamp = DateTime.UtcNow.AddDays(dayOffset);

        MainWindow!.CloseOverlay();

        await AccountManager.UpdateValueAsync(account, amount, timeStamp);
    }
}

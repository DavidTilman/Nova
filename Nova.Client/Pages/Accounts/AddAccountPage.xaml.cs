using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

using Nova;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AddAccountPage : Page
{
    public AddAccountPage()
    {
        this.InitializeComponent();
        AccountTypeComboBox.ItemsSource = Enum.GetValues(typeof(Nova.AccountType)).Cast<Nova.AccountType>().ToList();
    }

    private void ClearForm()
    {
        AccountNameTextBox.Text = string.Empty;
        AccountTypeComboBox.SelectedIndex = -1;
        AccountProviderTextBox.Text = string.Empty;
        AccountBalanceNumberBox.Value = 0;
    }

    private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
    {
        Nova.Account account = new Nova.Account
        {
            AccountName = AccountNameTextBox.Text,
            AccountType = (Nova.AccountType) AccountTypeComboBox.SelectedItem,
            AccountProvider = AccountProviderTextBox.Text,
            Balance = double.TryParse(AccountBalanceNumberBox.Text, out double value) ? value : 0,
            DateCreated = DateTime.UtcNow,
            Change = 0
        };

        try
        {
            Nova.Database.AccountManager.AddAccount(account);
            ContentDialog successDialog = new ContentDialog
            {
                Title = "Success",
                XamlRoot = this.XamlRoot,
                Content = "Account added successfully.",
                CloseButtonText = "Ok"
            };
            this.ClearForm();
            await successDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Error",
                XamlRoot = this.XamlRoot,
                Content = ex.Message,
                CloseButtonText = "Ok"
            };
            await errorDialog.ShowAsync();
        }
    }
}

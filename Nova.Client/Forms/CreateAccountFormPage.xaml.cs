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
public sealed partial class CreateAccountFormPage : Page
{
    MainWindow? MainWindow;
    public CreateAccountFormPage()
    {
        this.InitializeComponent();

        this.AccountTypeComboBox.ItemsSource = Enum.GetValues(typeof(AccountType));
    }

    private async void CreateAccountButton_Click(object sender, RoutedEventArgs e)
    {
        string accountName = AccountNameTextBox.Text;
        string accountProvider = AccountProviderTextBox.Text;
        AccountType accountType = (AccountType) AccountTypeComboBox.SelectedItem!;
        if (!double.TryParse(AccountBalanceNumberBox.Text, out double initialBalance))
        {
            return;
        }

        Account account = new Account()
        {
            AccountName = accountName,
            AccountProvider = accountProvider,
            AccountType = accountType,
            Balance = initialBalance
        };

        MainWindow!.CloseOverlay();

        await AccountManager.AddAccountAsync(account);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        MainWindow?.CloseOverlay();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is MainWindow window)
        {
            MainWindow = window;
        }
    }
}

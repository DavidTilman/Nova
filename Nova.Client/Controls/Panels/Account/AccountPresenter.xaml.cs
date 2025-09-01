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
using Nova.Database;

namespace Nova.Client.Controls;
public sealed partial class AccountPresenter : UserControl
{
    public Account Account;
    public AccountPresenter(Account account)
    {
        this.InitializeComponent();
        Account = account;
        this.Loaded += this.AccountPresenter_Loaded;

    }

    private async void AccountPresenter_Loaded(object sender, RoutedEventArgs e)
    {
        LastUpdateTextBlock.Text =
            await AccountManager.GetLastUpdateAsync(this.Account.ID) is DateTime lastUpdate
                ? $"{(DateTime.UtcNow - lastUpdate).Days}d"
                : "";

        BalanceChangeTextBlock.Text = Account.Change < 0
            ? $"▼{Account.FormattedChange}"
            : $"▲{Account.FormattedChange}";

        BalanceChangeTextBlock.Foreground = Account.Change < 0
            ? (SolidColorBrush) Application.Current.Resources["ErrorBrush"]
            : (SolidColorBrush) Application.Current.Resources["SuccessBrush"];
    }
}
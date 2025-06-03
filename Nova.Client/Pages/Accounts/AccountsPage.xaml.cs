using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Nova.Client.Controls;
using Nova.Database;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

using Nova;
using Nova.Client.Pages.Accounts;
using Microsoft.UI.Xaml.Media.Animation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AccountsPage : Page
{
    private Nova.Account? OverviewAccount = null;
    public AccountsPage()
    {
        this.InitializeComponent();
        List<Nova.Account> accounts = AccountManager.GetAccounts();
        foreach (Nova.Account account in accounts)
        {
            AccountCardGridView.Items.Add(new Controls.AccountCardUserControl(account));
        }
        AccountOverviewFrame.Navigate(typeof(AccountOverviewPageDefault), null, new DrillInNavigationTransitionInfo());
    }

    private void AccountCardGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        Nova.Account account = (e.ClickedItem as AccountCardUserControl)!.Account;
        if (OverviewAccount == null || OverviewAccount != account)
        {
            AccountOverviewFrame.Navigate(typeof(AccountOverviewPage), account, new DrillInNavigationTransitionInfo());
            OverviewAccount = account;
        }
        else
        {
            AccountOverviewFrame.Navigate(typeof(AccountOverviewPageDefault), null, new DrillInNavigationTransitionInfo());
            OverviewAccount = null;
        }
        AccountCardGridView.Focus(FocusState.Unfocused);
    }
}

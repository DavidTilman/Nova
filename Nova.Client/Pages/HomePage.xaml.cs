using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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
using Windows.Services.Store;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage : Page
{
    public HomePage()
    {
        this.InitializeComponent();
        Nova.Database.AccountManager.AccountChanged += this.AccountManager_AccountChanged;
    }

    private void AccountManager_AccountChanged(object? sender, EventArgs e) {
        if (HomePageFrame.Content is AccountsPage)
        {
            HomePageFrame.Navigate(typeof(AccountsPage), null, new DrillInNavigationTransitionInfo());
        }
    }

    private void HomePageNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        switch ((args.SelectedItem as NavigationViewItem)?.Content)
        {
            case "Accounts":
                HomePageFrame.Navigate(typeof(AccountsPage), null, new DrillInNavigationTransitionInfo());
                break;
            case "Investments":
                HomePageFrame.Navigate(typeof(InvestmentsPage), null, new DrillInNavigationTransitionInfo());
                break;
            default:
                break;
        }
    }

    private void AddAccountsPageFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        HomePageFrame.Navigate(typeof(AddAccountPage), null, new DrillInNavigationTransitionInfo());
    }
}

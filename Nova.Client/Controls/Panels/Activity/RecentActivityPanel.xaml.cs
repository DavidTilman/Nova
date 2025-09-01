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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Controls;
public sealed partial class RecentActivityPanel : UserControl
{
    public RecentActivityPanel()
    {
        this.InitializeComponent();
        this.Loaded += this.RecentActivityPanel_Loaded;
    }

    private async void RecentActivityPanel_Loaded(object sender, RoutedEventArgs e)
    {
        List<AccountEvent> events = await AccountManager.GetAllAccountEventsAsync();
        foreach (AccountEvent accountEvent in events)
        {
            AccountEventPresenterListView.Items.Add(new AccountEventPresenter(accountEvent));
        }
    }
}
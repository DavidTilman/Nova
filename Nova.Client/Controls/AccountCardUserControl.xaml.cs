using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Controls;
public sealed partial class AccountCardUserControl : UserControl
{
    public Account Account;

    public AccountCardUserControl(Account account)
    {
        this.InitializeComponent();
        Account = account;

        this.DispatcherQueue.TryEnqueue(async () =>
        {
            DateTime? lastUpdate = await Database.AccountManager.GetLastUpdateAsync(account);
            if (lastUpdate.HasValue)
            {
                LastUpdatedTextBlock.Text = $"{(DateTime.UtcNow - lastUpdate.Value).Days}d";
            }
        });

        if (account.Change < 0)
        {
            ChangeTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
            ChangeTextBlock.Text = $"▼ {account.Change:C} ({Math.Round(account.Change / ((account.Balance - account.Change) == 0 ? 1 : (account.Balance - account.Change)), 4):P})";

        }
        else if (account.Change > 0)
        {
            ChangeTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
            ChangeTextBlock.Text = $"▲ {account.Change:C} ({Math.Round(account.Change / ((account.Balance - account.Change) == 0 ? 1 : (account.Balance - account.Change)), 4):P})";
        }
        else
        {
            ChangeTextBlock.Text = $"{account.Change:C} ({Math.Round(account.Change / ((account.Balance - account.Change) == 0 ? 1 : (account.Balance - account.Change)), 4):P})";
        }
    }
}

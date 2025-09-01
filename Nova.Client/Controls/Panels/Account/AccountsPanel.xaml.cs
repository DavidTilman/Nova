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

namespace Nova.Client.Controls;
public sealed partial class AccountsPanel : UserControl
{
    public List<Account> Accounts
    {
        set => this.AccountPresenterListView.ItemsSource = value
                .Select(account => new AccountPresenter(account))
                .ToList();
    }
    public AccountsPanel()
    {
        this.InitializeComponent();
    }
}
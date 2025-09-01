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

namespace Nova.Client.Controls;
public sealed partial class RecentActivityPanel : UserControl
{
    public List<AccountEvent> Events
    {
        set
        {
            AccountEventPresenterListView.Items.Clear();
            foreach (AccountEvent accountEvent in value)
            {
                AccountEventPresenterListView.Items.Add(new AccountEventPresenter(accountEvent));
            }
        }
    }
    public RecentActivityPanel()
    {
        this.InitializeComponent();
    }
}
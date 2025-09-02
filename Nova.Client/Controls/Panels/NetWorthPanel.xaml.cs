using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Nova.Database;
using Syncfusion.UI.Xaml.Charts;

namespace Nova.Client.Controls;

public sealed partial class NetWorthPanel : UserControl
{
    public List<AccountEvent> AccountEvents
    {
        set
        {
            LineSeries lineSeries = new LineSeries()
            {
                ItemsSource = value,
                XBindingPath = "TimeStamp",
                YBindingPath = "NetWorth"
            };
            NetWorthChart.Series.Clear();
            NetWorthChart.Series.Add(lineSeries);
            NetWorthLabel.Text = AccountManager.FormattedNetWorth;
        }
    }
    public NetWorthPanel()
    {
        this.InitializeComponent();
    }
}
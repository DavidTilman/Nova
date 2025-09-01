using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Nova.Database;

using Syncfusion.UI.Xaml.Charts;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Nova.Client.Controls;

public class TypeBalancePair
{
    public required string Type { get; set; }
    public required double Balance { get; set; }
}

public sealed partial class WealthDistributionPanel : UserControl
{
    public List<Account> Accounts
    {
        set
        {
            List<TypeBalancePair> pairs = [];
            foreach (AccountType accountType in Enum.GetValues(typeof(AccountType)))
            {
                if (accountType == AccountType.None)
                    continue;
                double totalBalance = value.Where(a => a.AccountType == accountType).Sum(a => a.Balance);
                TypeBalancePair pair = new TypeBalancePair()
                {
                    Type = accountType.ToString(),
                    Balance = totalBalance
                };
                pairs.Add(pair);
            }

            PieSeries TypePieSeries = new PieSeries
            {
                ItemsSource = pairs,
                XBindingPath = "Type",
                YBindingPath = "Balance",
                ShowDataLabels = true
            };
            TypeBalancePieChart.Series.Add(TypePieSeries);
        }
    }
    public WealthDistributionPanel()
    {
        this.InitializeComponent();
    }
}
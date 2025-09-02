using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Nova.APIs;
using Nova.Database;

using Syncfusion.UI.Xaml.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Nova.Client.Controls;
public sealed partial class InvestmentPanel : UserControl
{
    public List<KrakenPosition> CryptoPositions
    {
        set => value
                .OrderBy(x => -x.Quantity)
                .ToList()
                .ForEach((x) => CryptoPresenterListView.Items.Add(new CrytpoPresenter(x)));
    }

    public List<Trading212Position> InvestmentPositions
    {
        set => value
                .OrderBy(x => -x.Gain)
                .ToList()
                .ForEach((x) => InvestmentPresenterListView.Items.Add(new InvestmentPresenter(x)));
    }

    public void ClearPositions()
    {
        InvestmentPresenterListView.Items.Clear();
    }
    public InvestmentPanel()
    {
        this.InitializeComponent();
    }
}
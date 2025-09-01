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
using Nova.APIs;

namespace Nova.Client.Controls;

public sealed partial class InvestmentTabCompact : UserControl
{
    public Trading212Position Position;
    public InvestmentTabCompact(Trading212Position position)
    {
        this.InitializeComponent();
        Position = position;

        GainTextBlock.Foreground = Position.Gain < 0
        ? (SolidColorBrush) Application.Current.Resources["ErrorBrush"]
        : (SolidColorBrush) Application.Current.Resources["SuccessBrush"];
    }
}
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

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
public sealed partial class TimeChangeTab : UserControl
{
    private readonly string Indicator;
    private readonly string Change;
    private readonly double ChangeValue;
    public TimeChangeTab(string indicator, double change)
    {
        this.InitializeComponent();
        this.Indicator = indicator;
        this.ChangeValue = change;
        this.Change = change.ToString("C");
        this.Loaded += this.TimeChangeTab_Loaded;
    }

    private void TimeChangeTab_Loaded(object sender, RoutedEventArgs e) => ChangeTextBlock.Foreground =
        ChangeValue < 0
            ? (SolidColorBrush) Application.Current.Resources["ErrorBrush"]
            : (SolidColorBrush) Application.Current.Resources["SuccessBrush"];
}
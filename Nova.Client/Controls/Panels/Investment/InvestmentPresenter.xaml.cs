using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System.Diagnostics;

using Nova.APIs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using System.Net.Http;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Controls;
public sealed partial class InvestmentPresenter : UserControl
{
    public Trading212Position Position;
    public InvestmentPresenter(Trading212Position position)
    {
        this.InitializeComponent();
        Position = position;
        GainTextBlock.Text = Position.FormattedGain;
        GainTextBlock.Foreground = Position.Gain < 0
            ? (SolidColorBrush) Application.Current.Resources["ErrorBrush"]
            : (SolidColorBrush) Application.Current.Resources["SuccessBrush"];
    }
}
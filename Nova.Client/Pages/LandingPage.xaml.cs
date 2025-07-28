using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;

using Nova.Database;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LandingPage : Page
{
    public LandingPage() => this.InitializeComponent();   

    public event EventHandler? LoadingComplete;

    private void Page_Loaded(object sender, RoutedEventArgs e) => this.AttemptLoadAsync();

    private  void AttemptLoadAsync()
    {

        LoadingTipTextBlock.Text = "Connecting to database...";

        int attempts = 0;
        bool connected = false;
        do
        {
            try
            {
                attempts++;
                connected = true;
            }
            catch (Exception ex)
            {
                LoadingTipTextBlock.Text = $"Failed to connect to database: \n{ex.Message}\nReconnecting... (Attempt {attempts})";
            }
        } 
        while (!connected);
        LoadingComplete?.Invoke(this, EventArgs.Empty);
    }
}

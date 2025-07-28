using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using Nova.Client.Pages;
using Nova.Database;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    readonly Storyboard FadeOutStoryboard = new Storyboard();
    readonly Storyboard FadeInStoryboard = new Storyboard();

    public MainWindow()
    {
        this.InitializeComponent();
        LandingPage landingPage = new LandingPage();
        MainWindowFrame.Content = landingPage;
        landingPage.LoadingComplete += this.MainWindow_LoadingComplete;

        FadeOutStoryboard.Children.Add(new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromSeconds(1)),
            AutoReverse = false,
            FillBehavior = FillBehavior.HoldEnd,

        });
        
        Storyboard.SetTarget(FadeOutStoryboard, MainWindowFrame);
        Storyboard.SetTargetProperty(FadeOutStoryboard, "Opacity");

        FadeInStoryboard.Children.Add(new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromSeconds(0.5)),  
            AutoReverse = false,
            FillBehavior = FillBehavior.HoldEnd,
        });

        Storyboard.SetTarget(FadeInStoryboard, MainWindowFrame);
        Storyboard.SetTargetProperty(FadeInStoryboard, "Opacity");
        FadeOutStoryboard.Completed += (s, e) =>
        {
            MainWindowFrame.Navigate(typeof(HomePage));
            FadeInStoryboard.Begin();
        };
    }

    private void MainWindow_LoadingComplete(object? sender, EventArgs e) => this.FadeOutStoryboard.Begin();

    public Frame GetMainWindowFrame => MainWindowFrame;
}

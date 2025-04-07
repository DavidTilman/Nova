using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

using Nova.lib;
using Nova.Pages;

namespace Nova;

public sealed partial class MainWindow : Window
{
    private bool inError = true;

    public MainWindow()
    {
        this.InitializeComponent();

        string query = "SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3";
        ManagementEventWatcher watcher = new ManagementEventWatcher(query);
        watcher.EventArrived += VolumeChangedEventHandler;
        watcher.Start();

        SetMainPage();
    }

    private void VolumeChangedEventHandler(object sender, EventArrivedEventArgs e)
    {
        SetMainPage();
    }

    private void SetMainPage()
    {
        if (!NovaDrive.DriveConnected)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                this.WindowFrame.Navigate(typeof(ErrorPage), "No N:/NOVA drive found.");
            });
            inError = true;
        }
        else
        {
            if (inError)
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    this.WindowFrame.Navigate(typeof(MainPage));
                });
            }
            inError = false;
        }
    }
}

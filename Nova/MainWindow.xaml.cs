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

using Nova.lib;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
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
                    this.Content = new ErrorPage("No N:\\NOVA drive was found.");
                });
                inError = true;
            }
            else
            {
                if (inError)
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        this.Content = new MainPage();
                    });
                }
                inError = false;
            }
        }
    }
}

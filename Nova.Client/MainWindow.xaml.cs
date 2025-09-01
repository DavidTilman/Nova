using Microsoft.UI.Windowing;
using Microsoft.UI;
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

using WinRT.Interop;
using Windows.Storage;
using Nova.APIs;
using Nova.Client.Controls;
using System.Diagnostics;
using Nova.Client.Forms;
using Microsoft.UI.Xaml.Media.Animation;
using System.Security.Principal;
using System.Xml.Serialization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JEaF5cXmRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXdfeHVcRWldUUF0WEZWYEk=");

        nint hwnd = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow? appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.MoveAndResize(new Windows.Graphics.RectInt32
        {
            Width = 1850,
            Height = 1000,
            X = 10,
            Y = 20
        });

        MainPage mainPage = new MainPage();
        MainContentFrame.Content = mainPage;

        MainWindowSidePanel.OpenPaymentForm += this.MainWindow_OpenPaymentForm;
        MainWindowSidePanel.OpenTransferForm += this.MainWindow_OpenTransferForm;
        MainWindowSidePanel.OpenUpdateForm += this.MainWindowSidePanel_OpenUpdateForm;
        MainWindowSidePanel.OpenIncomeForm += this.MainWindowSidePanel_OpenIncomeForm;
        MainWindowSidePanel.OpenInterestForm += this.MainWindowSidePanel_OpenInterestForm;

        this.DispatcherQueue.TryEnqueue(async () =>
        {
            List<KrakenPosition> krakenBalance = await Kraken.GetBalanceAsync();
            if (krakenBalance != null)
            {
                mainPage.CryptoPositions = krakenBalance;
                MainWindowSidePanel.CryptoPositions = krakenBalance;
            }
        });

        this.DispatcherQueue.TryEnqueue(async () =>
        {
            List<Trading212Position> trading212Positions = await Trading212.GetPositionsAsync();
            if (trading212Positions != null)
            {
                mainPage.InvestmentPositions = trading212Positions;
                MainWindowSidePanel.InvestmentPositions = trading212Positions;
            }
        });

        this.DispatcherQueue.TryEnqueue(async () =>
        {
            List<Account> accounts = await Database.AccountManager.GetAccountsAsync();
            if (accounts != null)
            {
                MainWindowSidePanel.Accounts = accounts;
                mainPage.Accounts = accounts;
            }
        });
    }

    private void OpenOverlayPage(Type pageType)
    {
        OverlayFrame.Navigate(pageType, this, new SuppressNavigationTransitionInfo());
    }

    private void MainWindowSidePanel_OpenInterestForm(object? sender, EventArgs e)
    {
        this.OpenOverlayPage(typeof(InterestFormPage));
    }

    private void MainWindowSidePanel_OpenIncomeForm(object? sender, EventArgs e)
    {
        this.OpenOverlayPage(typeof(IncomeFormPage));
    }

    private void MainWindowSidePanel_OpenUpdateForm(object? sender, EventArgs e)
    {
        this.OpenOverlayPage(typeof(UpdateFormPage));
    }

    private void MainWindow_OpenPaymentForm(object? sender, EventArgs e)
    {
        this.OpenOverlayPage(typeof(PaymentFormPage));
    }

    private void MainWindow_OpenTransferForm(object? sender, EventArgs e)
    {
        this.OpenOverlayPage(typeof(TransferFormPage));
    }

    public void CloseOverlay()
    {
        OverlayFrame.Content = null;
    }
}
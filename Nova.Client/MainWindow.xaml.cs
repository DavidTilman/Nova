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
using Nova.Database;

namespace Nova.Client;
public sealed partial class MainWindow : Window
{
    private readonly MainPage MainPage = new MainPage();
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

        MainWindowSidePanel.OpenPaymentForm += this.MainWindow_OpenPaymentForm;
        MainWindowSidePanel.OpenTransferForm += this.MainWindow_OpenTransferForm;
        MainWindowSidePanel.OpenUpdateForm += this.MainWindowSidePanel_OpenUpdateForm;
        MainWindowSidePanel.OpenIncomeForm += this.MainWindowSidePanel_OpenIncomeForm;
        MainWindowSidePanel.OpenInterestForm += this.MainWindowSidePanel_OpenInterestForm;
        MainWindowSidePanel.OpenCreateForm += this.MainWindowSidePanel_OpenCreateForm;

        AccountManager.AccountChanged += (object? sender, EventArgs e) => this.ResetContent();

        this.ResetContent();
    }

    private void MainWindowSidePanel_OpenCreateForm(object? sender, EventArgs e)
    {
        this.OpenOverlayPage(typeof(CreateAccountFormPage));
    }

    private async void ResetContent()
    {
        this.MainPage.InvestmentPanel.ClearPositions();
        this.MainWindowSidePanel.LoadDMQYChangesPanel();

        List<KrakenPosition> krakenBalance = await Kraken.GetBalanceAsync();
        if (krakenBalance != null)
        {
            this.MainPage.CryptoPositions = krakenBalance;
            MainWindowSidePanel.CryptoPositions = krakenBalance;
        }

        List<Trading212Position> trading212Positions = await Trading212.GetPositionsAsync();
        if (trading212Positions != null)
        {
            this.MainPage.InvestmentPositions = trading212Positions;
            MainWindowSidePanel.InvestmentPositions = trading212Positions;
        }

        List<Account> accounts = await AccountManager.GetAccountsAsync();
        if (accounts != null)
        {
            MainWindowSidePanel.Accounts = accounts;
            this.MainPage.Accounts = accounts;
        }

        List<AccountEvent> accountEvents = await AccountManager.GetAllAccountEventsAsync();
        if (accountEvents != null)
        {
            this.MainPage.AccountEvents = accountEvents;
        }

        MainContentFrame.Content = MainPage;
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
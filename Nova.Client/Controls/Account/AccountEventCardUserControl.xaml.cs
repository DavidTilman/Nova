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
using Microsoft.UI.Xaml.Documents;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Controls;

public sealed partial class AccountEventCardUserControl : UserControl
{
    readonly AccountEvent AccountEvent;

    public string CentralText
    {
        get
        {
            switch (AccountEvent.EventType)
            {
                case AccountEventType.Created:
                    return "Account Created";
                case AccountEventType.Income:
                    SymbolFontIcon.Foreground = (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                    CentralTextBlock.Foreground = (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                    return $"+{AccountEvent.Value??0:C} • {AccountEvent.SecondaryAccountName}";
                case AccountEventType.Transfer:
                    if (AccountEvent.Value < 0)
                    {
                        SymbolFontIcon.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
                        CentralTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
                        return $"+{AccountEvent.Value ?? 0:C} • {AccountEvent.SecondaryAccountName}";
                    }
                    else
                    {
                        SymbolFontIcon.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
                        CentralTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
                        return $"+{AccountEvent.Value ?? 0:C} • {AccountEvent.SecondaryAccountName}";
                    }
                case AccountEventType.Payment:
                    SymbolFontIcon.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
                    CentralTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorCriticalBrush"];
                    return $"-{AccountEvent.Value ?? 0:C} • {AccountEvent.SecondaryAccountName}";
                case AccountEventType.Interest:
                    SymbolFontIcon.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
                    CentralTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources["SystemFillColorSuccessBrush"];
                    return $"+{AccountEvent.Value ?? 0:C} • {AccountEvent.SecondaryAccountName}";
                case AccountEventType.UpdateValue:
                    if (AccountEvent.OldBalance == -1)
                        throw new ArgumentException("Old balance must be set for UpdateValue events.");
                    double diff = AccountEvent.NewBalance - AccountEvent.OldBalance;
                    char sign = diff >= 0 ? '+' : '-';
                    SymbolFontIcon.Foreground = (SolidColorBrush) Application.Current.Resources[diff >= 0 ? "SystemFillColorSuccessBrush" : "SystemFillColorCriticalBrush"];
                    CentralTextBlock.Foreground = (SolidColorBrush) Application.Current.Resources[diff >= 0 ? "SystemFillColorSuccessBrush" : "SystemFillColorCriticalBrush"];
                    return $"{sign}{Math.Abs(diff):C} • {AccountEvent.SecondaryAccountName}";
                default:
                    return "Event";
            }
        }
    }

    public string AccountEventGlyph => AccountEvent.EventType switch
    {
        AccountEventType.Created => "\uE8A7",
        AccountEventType.Income => "\uE710",
        AccountEventType.Transfer => "\uE8AB",
        AccountEventType.Payment => "\uE8C7",
        AccountEventType.Interest => "\uE825",
        AccountEventType.UpdateValue => "\uE895",
        _ => "\uE8A7",
    };

    public AccountEventCardUserControl(AccountEvent accountEvent)
    {
        this.InitializeComponent();
        AccountEvent = accountEvent;
        AccountNameTextBlock.Text = string.Empty;
    }

    public AccountEventCardUserControl(AccountEvent accountEvent, bool displayAccountName)
    {
        this.InitializeComponent();
        AccountEvent = accountEvent;
        AccountNameTextBlock.Text = displayAccountName ? accountEvent.AccountName ?? "Unknown Account" : string.Empty;
    }
}

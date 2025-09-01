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
using Nova.Database;

namespace Nova.Client.Controls;

public sealed partial class NetWorthPanel : UserControl
{
    public List<AccountEvent> AccountEvents = AccountManager.GetAllAccountEventsAsync().Result; //.Append(new AccountEvent() { EventType=AccountEventType.None, NetWorth=AccountManager.NetWorth, NewBalance=-1, OldBalance=-1, TimeStamp=DateTime.UtcNow}).ToList();
    public NetWorthPanel()
    {
        this.InitializeComponent();
    }
}
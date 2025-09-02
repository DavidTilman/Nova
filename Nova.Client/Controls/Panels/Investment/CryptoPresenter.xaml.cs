using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Nova.APIs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Nova.Client.Controls;
public sealed partial class CrytpoPresenter : UserControl
{
    readonly KrakenPosition Position;
    readonly double RoundedBalance;
    public CrytpoPresenter(KrakenPosition position)
    {
        this.InitializeComponent();
        Position = position;
        RoundedBalance = Math.Round(position.Quantity, 5);
    }
}
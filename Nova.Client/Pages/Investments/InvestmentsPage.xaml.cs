using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Nova.APIs;
using Nova.Client.Controls;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class InvestmentsPage : Page
{
    public InvestmentsPage()
    {
        this.InitializeComponent();
        DispatcherQueue.TryEnqueue(async () =>
        {
            PositionListView.Items.Add(new InvestmentPositionCard());
            List<Trading212Position> positions = await Trading212.GetPositions();
            foreach (Trading212Position position in positions)
            {
                InvestmentPositionCard card = new InvestmentPositionCard(position);
                PositionListView.Items.Add(card);
            }
        });
    }
}

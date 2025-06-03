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
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AddIncomeForm : Page
{
    public double IncomeValue
        => double.TryParse(IncomeNumberBox.Text, out double value) ? value : 0.0;

    public string IncomeSource => SourceTextBox.Text;
    public AddIncomeForm()
    {
        this.InitializeComponent();
        IncomeNumberBox.Value = 0;
    }

    private void IncomeNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        SavedAmountTextBlock.Text = (args.NewValue * 0.1).ToString("C");
        InvestedAmountTextBlock.Text = (args.NewValue * 0.15).ToString("C");
    }
}

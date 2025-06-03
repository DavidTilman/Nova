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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TransferFundsForm : Page
{
    public double TransferAmount 
        => double.TryParse(TransferNumberBox.Text, out double value) ? value : 0.0;

    public int ToAccountID
    {
        get
        {
            if (ToAccountComboBox.SelectedItem is string selectedItem)
            {
                string[] parts = selectedItem.Split(" - ");
                if (parts.Length > 0 && int.TryParse(parts[0], out int id))
                {
                    return id;
                }
            }

            return -1;
        }
    }

    public TransferFundsForm(Account account)
    {
        this.InitializeComponent();
        List<Account> accounts = [];
        ToAccountComboBox.ItemsSource = from acc in Nova.Database.AccountManager.GetAccounts()
                                        where acc.ID != account.ID
                                        select $"{acc.ID} - {acc.AccountName} - {acc.AccountProvider}";
    }
}

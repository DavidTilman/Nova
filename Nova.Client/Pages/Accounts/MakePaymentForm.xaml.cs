using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Nova.Client.Pages.Accounts;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MakePaymentForm : Page
{
    Account Account { get; set; }

    public double PaymentAmount =>
        double.TryParse(PaymentAmountNumberBox.Text, out double value) ? value : 0.0;

    public string PaymentPayee => PayeeTextBox.Text;

    List<string>? Payees { get; set; } = null;
    public MakePaymentForm(Account account)
    {
        this.InitializeComponent();
        Account = account;
        Task.Run(async () => Payees = await Database.AccountManager.GetPayeesAsync(account));
    }

    private void PayeeTextBox_TextChanged(object sender, AutoSuggestBoxTextChangedEventArgs e)
    {
        if(Payees == null)
        {
            return;
        }

        if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var suitableItems = new List<string>();
            var splitText = (sender as AutoSuggestBox).Text.ToLower().Split(" ");
            foreach (var payee in Payees)
            {
                var found = splitText.All((key) =>
                {
                    return payee.ToLower().Contains(key);
                });
                if (found)
                {
                    suitableItems.Add(payee);
                }
            }
            (sender as AutoSuggestBox).ItemsSource = suitableItems;
        }

    }
}

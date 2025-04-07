using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.lib;
internal class AccountsManager
{
    public static List<Account> Accounts => new List<Account>
    {
        new Account
        {
            AccountName = "Current",
            AccountProvider = "Bank of America",
            AccountType = AccountType.Current
        },
        new Account
        {
            AccountName = "Savings",
            AccountProvider = "Chase",
            AccountType = AccountType.Savings
        },
        new Account
        {
            AccountName = "Investment",
            AccountProvider = "Fidelity",
            AccountType = AccountType.Investment
        }
    };
}

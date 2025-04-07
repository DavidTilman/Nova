using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.lib;

public enum AccountType
{
    None,
    Current,
    Savings,
    Investment
}
public class Account
{
    public string AccountName { get; set; }

    public double Balance { get; set; }

    public string BalanceString => Balance.ToString("C2");
    public string AccountProvider { get; set; }
    public AccountType AccountType { get; set; }

    public Account()
    {
        AccountName = string.Empty;
        AccountProvider = string.Empty;
        AccountType = AccountType.None;
    }
}

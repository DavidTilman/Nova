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
    public required string AccountName { get; set; }
    public required string AccountProvider { get; set; }
    public required AccountType AccountType { get; set; }
}

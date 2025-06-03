using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova;

public enum AccountEventType
{
    Created,
    Income,
    Transfer,
    Payment,
    Interest,
    UpdateValue,
}
public class AccountEvent
{
    public string? AccountName { get; set; }
    public required AccountEventType EventType { get; set; }
    public double? Value { get; set; } = null;
    public string? SecondaryAccountName { get; set; } = null;
    public required DateTime TimeStamp { get; set; }
    public required double NewBalance { get; set; }
    public required double OldBalance { get; set; } = -1;
    public required double NetWorth { get; set; } = -1;
    public string FormattedNewBalance => this.NewBalance.ToString("C");
}

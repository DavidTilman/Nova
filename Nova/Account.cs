using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova;

public enum AccountType
{
    None,
    Current,
    Savings,
    Investment,
    Asset,
}
public class Account
{
    public int ID { get; set; }
    public required string AccountName { get; set; }
    public required AccountType AccountType { get; set; }
    public required string AccountProvider { get; set; }
    public required double Balance { get; set; }
    public required DateTime DateCreated { get; set; }

    public required double Change { get; set; }
    public string FormattedBalance => $"{this.Balance:C}";
    public string FormattedChange => $"{this.Change:C}";
    public double FractionOfNetWorth => this.Balance / (Database.AccountManager.NetWorth == 0 ? 1 : Database.AccountManager.NetWorth);

    public string FormattedPercentageOfNetWorth => $"{this.FractionOfNetWorth:P2} of your networth";

    public static Account FromReader(Npgsql.NpgsqlDataReader reader) => new Account()
    {
        ID = reader.GetInt32(0),
        AccountName = reader.GetString(1),
        AccountProvider = reader.GetString(2),
        AccountType = (AccountType) reader.GetInt32(3),
        Balance = reader.GetDouble(4),
        DateCreated = reader.GetDateTime(5),
        Change = reader.GetDouble(6)
    };
}

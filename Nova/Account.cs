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
    public string AccountName { get; set; }
    public AccountType AccountType { get; set; }
    public string AccountProvider { get; set; }
    public double Balance { get; set; }
    public DateTime DateCreated { get; set; }
    public double Change { get; set; }
    public string FormattedBalance => $"{this.Balance:C}";
    public string FormattedChange => $"{this.Change:C}";
    public double FractionOfNetWorth => this.Balance / (Database.AccountManager.NetWorth == 0 ? 1 : Database.AccountManager.NetWorth);

    public string FormattedPercentageOfNetWorth => $"{this.FractionOfNetWorth:P2}";

    public static Account FromReader(Npgsql.NpgsqlDataReader reader) => new Account()
    {
        ID = reader.GetInt32(0),
        AccountName = reader.GetString(1),
        AccountProvider = reader.GetString(2),
        AccountType = (AccountType) reader.GetInt32(3),
        Balance = Convert.ToDouble(reader.GetDecimal(4)),
        DateCreated = reader.GetDateTime(5),
        Change = Convert.ToDouble(reader.GetDecimal(6))
    };

    public override string ToString() => $"{this.AccountName} {this.AccountProvider} ({this.AccountType}): {this.FormattedBalance} [{this.DateCreated}]";

    public Account()
    {
        this.ID = -1;
        this.AccountName = string.Empty;
        this.AccountProvider = string.Empty;
        this.Balance = 0;
        this.AccountType = AccountType.None;
        this.DateCreated = DateTime.UtcNow;
        this.Change = 0;
    }
}
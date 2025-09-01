using Npgsql;
using Nova.Database;

namespace Nova;

public enum AccountEventType
{
    Created,
    Income,
    Transfer,
    Payment,
    Interest,
    UpdateValue,
    None
}
public class AccountEvent
{
    public string? AccountName { get; set; }
    public AccountEventType EventType { get; set; }
    public double? Value { get; set; } = null;
    public string? SecondaryAccountName { get; set; } = null;
    public DateTime TimeStamp { get; set; }
    public double NewBalance { get; set; }
    public double OldBalance { get; set; } = -1;
    public double NetWorth { get; set; } = -1;
    public string FormattedValue => this.Value.HasValue ? this.Value.Value.ToString("C") : "";
    public string FormattedNewBalance => this.NewBalance.ToString("C");

    public static async Task<AccountEvent> FromReader(NpgsqlDataReader reader)
    {
        return new AccountEvent
        {
            AccountName = (await AccountManager.GetAccountAsync(reader.GetInt32(1)))!.AccountName,
            EventType = (AccountEventType) reader.GetInt32(2),
            Value = reader.IsDBNull(3) ? null : Convert.ToDouble(reader.GetDecimal(3)),
            SecondaryAccountName = reader.IsDBNull(4) ? null : reader.GetString(4),
            TimeStamp = reader.GetDateTime(5),
            NewBalance = Convert.ToDouble(reader.GetDecimal(6)),
            OldBalance = Convert.ToDouble(reader.GetDecimal(7)),
            NetWorth = Convert.ToDouble(reader.GetDecimal(8))
        };
    }

    public static NpgsqlCommand InsertCommand(Account account, AccountEvent accountEvent)
    {
        string query = """
            INSERT INTO account_events (account_id, type, value, description, timestamp, new_balance, old_balance, net_worth) 
            VALUES (@PrimaryAccountId, @Type, @Value, @SecondaryText, @TimeStamp, @NewBalance, @OldBalance, @NetWorth);
        """;

        NpgsqlCommand command = new NpgsqlCommand(query);
        command.Parameters.Add("@PrimaryAccountId", NpgsqlTypes.NpgsqlDbType.Integer).Value = account.ID;
        command.Parameters.Add("@Type", NpgsqlTypes.NpgsqlDbType.Integer).Value = (int) accountEvent.EventType;
        command.Parameters.Add("@Value", NpgsqlTypes.NpgsqlDbType.Numeric).Value = accountEvent.Value.HasValue ? Convert.ToDecimal(accountEvent.Value.Value) : (object) DBNull.Value;
        command.Parameters.Add("@SecondaryText", NpgsqlTypes.NpgsqlDbType.Text).Value = (object?) accountEvent.SecondaryAccountName ?? DBNull.Value;
        command.Parameters.Add("@TimeStamp", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = accountEvent.TimeStamp;
        command.Parameters.Add("@NewBalance", NpgsqlTypes.NpgsqlDbType.Numeric).Value = Convert.ToDecimal(accountEvent.NewBalance);
        command.Parameters.Add("@OldBalance", NpgsqlTypes.NpgsqlDbType.Numeric).Value = Convert.ToDecimal(accountEvent.OldBalance);
        command.Parameters.Add("@NetWorth", NpgsqlTypes.NpgsqlDbType.Numeric).Value = Convert.ToDecimal(accountEvent.NetWorth);

        return command;
    }

    public static AccountEvent IncomeEvent(Account account, string source, double value, double networth, DateTime timeStamp)
    {
        return new AccountEvent
        {
            EventType = AccountEventType.Income,
            Value = value,
            SecondaryAccountName = source,
            TimeStamp = timeStamp,
            NewBalance = account.Balance + value,
            OldBalance = account.Balance,
            NetWorth = networth
        };
    }

    public static (AccountEvent to, AccountEvent from) TransferAccountEvents(Account accountTo, Account accountFrom, double value, double networth, DateTime timeStamp)
    {
        return (
        new AccountEvent
        {
            EventType = AccountEventType.Transfer,
            Value = value,
            SecondaryAccountName = accountFrom.AccountName,
            TimeStamp = timeStamp,
            NewBalance = accountTo.Balance + value,
            OldBalance = accountTo.Balance,
            NetWorth = networth
        },

        new AccountEvent
        {
            EventType = AccountEventType.Transfer,
            Value = -value,
            SecondaryAccountName = accountTo.AccountName,
            TimeStamp = timeStamp,
            NewBalance = accountFrom.Balance - value,
            OldBalance = accountFrom.Balance,
            NetWorth = networth
        }
    );
    }

    public static AccountEvent PaymentEvent(Account account, string source, double value, double networth, DateTime timeStamp)
    {
        return new AccountEvent
        {
            EventType = AccountEventType.Payment,
            Value = -value,
            SecondaryAccountName = source,
            TimeStamp = timeStamp,
            NewBalance = account.Balance - value,
            OldBalance = account.Balance,
            NetWorth = networth
        };
    }

    public static AccountEvent InterestEvent(Account account, double value, double networth, DateTime timeStamp)
    {
        return new AccountEvent
        {
            EventType = AccountEventType.Interest,
            Value = value,
            TimeStamp = timeStamp,
            NewBalance = account.Balance + value,
            OldBalance = account.Balance,
            NetWorth = networth
        };
    }

    public static AccountEvent UpdateValueEvent(Account account, double newValue, double networth)
    {
        return new AccountEvent
        {
            EventType = AccountEventType.UpdateValue,
            Value = newValue,
            TimeStamp = DateTime.UtcNow,
            NewBalance = newValue,
            OldBalance = account.Balance,
            NetWorth = networth
        };
    }

    public override string ToString()
    {
        string valueText = this.Value.HasValue ? $"{this.Value.Value:C}" : "N/A";
        string secondaryText = string.IsNullOrEmpty(this.SecondaryAccountName) ? "N/A" : this.SecondaryAccountName;
        return $"{this.TimeStamp:yyyy-MM-dd HH:mm:ss} - {this.EventType} - {this.AccountName} - {secondaryText} - {valueText} - New Balance: {this.FormattedNewBalance} - Old Balance: {this.OldBalance:C} - Net Worth: {this.NetWorth:C}";
    }
}
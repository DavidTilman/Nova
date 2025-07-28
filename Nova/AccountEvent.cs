using Microsoft.Identity.Client;

using Npgsql;

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

    public static AccountEvent FromReader(NpgsqlDataReader reader) => new AccountEvent
    {
        EventType = (AccountEventType) reader.GetInt32(2),
        Value = reader.IsDBNull(3) ? null : reader.GetDouble(3),
        SecondaryAccountName = reader.IsDBNull(4) ? null : reader.GetString(4),
        TimeStamp = reader.GetDateTime(5),
        NewBalance = reader.GetDouble(6),
        OldBalance = reader.GetDouble(7),
        NetWorth = reader.GetDouble(8)
    };

    public static NpgsqlCommand InsertCommand(Account account, AccountEvent accountEvent)
    {
        string query =
            """
                INSERT INTO Account_Events (PrimaryAccountId, Type, Value, SecondaryText, TimeStamp, NewBalance, OldBalance, NetWorth) 
                VALUES (@PrimaryAccountId, @Type, @Value, @SecondaryText, @TimeStamp, @NewBalance, @OldBalance, @NetWorth);
            """;

        NpgsqlCommand command = new NpgsqlCommand(query);
        command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
        command.Parameters.AddWithValue("@Type", accountEvent.EventType);
        command.Parameters.AddWithValue("@Value", (object?) accountEvent.Value ?? DBNull.Value);
        command.Parameters.AddWithValue("@SecondaryText", (object?) accountEvent.SecondaryAccountName ?? DBNull.Value);
        command.Parameters.AddWithValue("@TimeStamp", accountEvent.TimeStamp);
        command.Parameters.AddWithValue("@NewBalance", accountEvent.NewBalance);
        command.Parameters.AddWithValue("@OldBalance", accountEvent.OldBalance);
        command.Parameters.AddWithValue("@NetWorth", accountEvent.NetWorth);

        return command;
    }

    public static AccountEvent IncomeEvent(Account account, string source, double value, double networth) => new AccountEvent
    {
        EventType = AccountEventType.Income,
        Value = value,
        SecondaryAccountName = source,
        TimeStamp = DateTime.UtcNow,
        NewBalance = account.Balance + value,
        OldBalance = account.Balance,
        NetWorth = networth
    };

    public static (AccountEvent to, AccountEvent from) TransferAccountEvents(Account accountTo, Account accountFrom, double value, double networth) =>  (
        new AccountEvent
        {
            EventType = AccountEventType.Transfer,
            Value = value,
            SecondaryAccountName = accountFrom.AccountName,
            TimeStamp = DateTime.UtcNow,
            NewBalance = accountTo.Balance + value,
            OldBalance = accountTo.Balance,
            NetWorth = networth
        },

        new AccountEvent
        {
            EventType = AccountEventType.Transfer,
            Value = -value,
            SecondaryAccountName = accountTo.AccountName,
            TimeStamp = DateTime.UtcNow,
            NewBalance = accountFrom.Balance - value,
            OldBalance = accountFrom.Balance,
            NetWorth = networth
        }
    );

    public static AccountEvent PaymentEvent(Account account, string source, double value, double networth) => new AccountEvent
    {
        EventType = AccountEventType.Payment,
        Value = -value,
        SecondaryAccountName = source,
        TimeStamp = DateTime.UtcNow,
        NewBalance = account.Balance - value,
        OldBalance = account.Balance,
        NetWorth = networth
    };

    public static AccountEvent InterestEvent(Account account, double value, double networth) => new AccountEvent
    {
        EventType = AccountEventType.Interest,
        Value = value,
        TimeStamp = DateTime.UtcNow,
        NewBalance = account.Balance + value,
        OldBalance = account.Balance,
        NetWorth = networth
    };

    public static AccountEvent UpdateValueEvent(Account account, double newValue, double networth) => new AccountEvent
    {
        EventType = AccountEventType.UpdateValue,
        Value = newValue,
        TimeStamp = DateTime.UtcNow,
        NewBalance = newValue,
        OldBalance = account.Balance,
        NetWorth = networth
    };
}

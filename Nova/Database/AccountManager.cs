using Microsoft.Data.SqlClient;

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Npgsql;
using Npgsql.Replication.PgOutput.Messages;

namespace Nova.Database;
public static class AccountManager
{
    public static event EventHandler? AccountChanged;
    public static double NetWorth => GetAccountsAsync().Result.Sum(a => a.Balance);
    public static string FormattedNetWorth => NetWorth.ToString("C");

    public static async void AddAccount(Account account)
    {
        Cache.Accounts = null;

        string query =
            """
                INSERT INTO Accounts (Name, Provider, Type, Balance, DateCreated, Change) 
                VALUES (@Name, @Provider, @Type, @Balance, @DateCreated, 0); 
                SELECT SCOPE_IDENTITY();
            """;

        int newAccountId;

        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();
            
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Name", account.AccountName);
                command.Parameters.AddWithValue("@Provider", account.AccountProvider);
                command.Parameters.AddWithValue("@Type", (byte) account.AccountType);
                command.Parameters.AddWithValue("@Balance", account.Balance);
                command.Parameters.AddWithValue("@DateCreated", DateTime.UtcNow);
                
                object? result = await command.ExecuteScalarAsync();
                newAccountId = Convert.ToInt32(result);
            }

            connection.Close(); 
        }

        account.ID = newAccountId;
        
        AccountEvent creation = new AccountEvent
        {
            EventType = AccountEventType.Created,
            NewBalance = account.Balance,
            OldBalance = 0,
            TimeStamp = account.DateCreated,
            NetWorth = NetWorth,
        };

        AddEventAsync(account, creation);
    }

    public static async Task<List<Account>> GetAccountsAsync()
    {
        if (Cache.Accounts != null) return Cache.Accounts;

        string query = 
            """
                SELECT * 
                FROM Accounts;
            """;

        List<Account> accounts = [];

        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                using NpgsqlDataReader reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    Account acc = Account.FromReader(reader);

                    accounts.Add(acc);
                }
            }

            await connection.CloseAsync();
        }

        Cache.Accounts = accounts;
        
        return accounts;
    }

    private static async void AddEventAsync(Account account, AccountEvent accountEvent)
    {
        Cache.Accounts = null;
        Cache.SpeficAccountEvents.Remove(account.ID);
        Cache.AllAccountEvents = null;

        using NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString);

        await connection.OpenAsync();

        using (NpgsqlCommand command = AccountEvent.InsertCommand(account, accountEvent))
        {
            command.Connection = connection;
            await command.ExecuteNonQueryAsync();
        }

        await connection.CloseAsync();
    }

    public static async Task<List<AccountEvent>> GetAccountEventsAsync(Account account)
    {
        if (Cache.SpeficAccountEvents.TryGetValue(account.ID, out List<AccountEvent>? cachedEvents)) return cachedEvents; 

        string query =
            """
                SELECT * 
                FROM Account_Events 
                WHERE 
                    PrimaryAccountId = @PrimaryAccountId 
                ORDER BY 
                    TimeStamp DESC;
            """;

        List<AccountEvent> events = [];  
        
        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();  
            
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);

                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                
                while (reader.Read())
                {
                    AccountEvent accountEvent = AccountEvent.FromReader(reader);
                    events.Add(accountEvent);
                }
            }

            await connection.CloseAsync();
        }

        Cache.SpeficAccountEvents.Add(account.ID, events);
        return events;
    }

    public static async void AddIncomeAsync(Account account, double value, string source)
    {
        Cache.Accounts = null;
        Cache.AllAccountEvents?.Clear();
        Cache.SpeficAccountEvents.Remove(account.ID);

        string query = 
            """
                UPDATE Accounts 
                SET 
                    Balance = Balance + @Value, 
                    Change = @Value 
                WHERE 
                    Id = @Id;
            """;

        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Value", value);
                command.Parameters.AddWithValue("@Id", account.ID);
                await command.ExecuteNonQueryAsync();
            }

            await connection.CloseAsync();
        }

        AccountEvent incomeEvent = AccountEvent.IncomeEvent(account, source, value, NetWorth); 

        AddEventAsync(account, incomeEvent);
        
        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async Task<Account> GetAccount(int id)
    {
        if (Cache.Accounts == null) await GetAccountsAsync();

        Account? account = Cache.Accounts!.First(a => a.ID == id);        
        
        return account;
    }

    public static async void MakeTransferAsync(Account accountTo, Account accountFrom, double value)
    {
        Cache.Accounts = null;
        Cache.AllAccountEvents?.Clear();
        Cache.SpeficAccountEvents.Remove(accountTo.ID);
        Cache.SpeficAccountEvents.Remove(accountFrom.ID);
        
        string query = 
            """
                UPDATE Accounts 
                SET 
                    Balance = Balance + @Value, 
                    Change = @Value 
                WHERE 
                    Id = @ToId; 
                
                UPDATE Accounts 
                SET 
                    Balance = Balance - @Value, 
                    Change = @Value * -1 
                WHERE 
                    Id = @FromId;
            """;

        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Value", value);
                command.Parameters.AddWithValue("@ToId", accountTo.ID);
                command.Parameters.AddWithValue("@FromId", accountFrom.ID);
                await command.ExecuteNonQueryAsync();
            }

            await connection.CloseAsync();
        }

        (AccountEvent eventTo, AccountEvent eventFrom) transferEvents = AccountEvent.TransferAccountEvents(accountTo, accountFrom, value, NetWorth);


        AddEventAsync(accountTo, transferEvents.eventFrom);

        AddEventAsync(accountFrom, transferEvents.eventTo);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async Task<List<AccountEvent>> GetAccountEventsAsync(int n)
    {
        if (Cache.AllAccountEvents != null && Cache.AllAccountEvents.Count >= n) return Cache.AllAccountEvents.Take(n).ToList();
        
        string query = 
            """
                SELECT TOP (@N) * 
                FROM Account_Events 
                ORDER BY TimeStamp DESC;
            """;

        List<AccountEvent> Account_Events = [];
        
        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@N", n);

                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

                while (reader.Read())
                {
                    AccountEvent accountEvent = AccountEvent.FromReader(reader);

                    Account_Events.Add(accountEvent);
                }
            }

            await connection.CloseAsync();
        }

        return Account_Events;
    }

    public static async Task<List<AccountEvent>> GetAllAccountEventsAsync()
    {

        if (Cache.AllAccountEvents != null)
        {
            return [.. Cache.AllAccountEvents];
        }

        string query =
            """
                SELECT * 
                FROM Account_Events 
                ORDER BY TimeStamp DESC;
            """;

        List<AccountEvent> accountEvents = [];
        
        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))    
            {
                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                
                while (reader.Read())
                {
                    int accountId = reader.GetInt32(1);
                    AccountEventType eventType = (AccountEventType) reader.GetInt32(2);
                    double? value = reader.IsDBNull(3) ? null : reader.GetDouble(3);
                    string? secondaryAccountName = reader.IsDBNull(4) ? null : reader.GetString(4);
                    DateTime timeStamp = reader.GetDateTime(5);
                    double newBalance = reader.GetDouble(6);
                    double oldbalance = reader.GetDouble(7);
                    double netWorth = reader.GetDouble(8);
                    AccountEvent accountEvent = new AccountEvent
                    {
                        AccountName = (await GetAccount(accountId)).AccountName,
                        EventType = eventType,
                        Value = value,
                        SecondaryAccountName = secondaryAccountName,
                        TimeStamp = timeStamp,
                        NewBalance = newBalance,
                        OldBalance = oldbalance,
                        NetWorth = netWorth
                    };

                    accountEvents.Add(accountEvent);

                }
            }
            await connection.CloseAsync();
        }

        return accountEvents;
    }

    public static async Task<List<string>> GetPayeesAsync(Account account)
    {
        string query =
            """
                SELECT DISTINCT SecondaryText 
                FROM Account_Events 
                WHERE 
                    PrimaryAccountId = @PrimaryAccountId
                    AND 
                    Type = @Type;
            """;

        List<string> payees = [];

        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();
            
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
                command.Parameters.AddWithValue("@Type", (int) AccountEventType.Payment);

                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    string payee = reader.GetString(0);
                    payees.Add(payee);
                }
            }

            await connection.CloseAsync();
        }

        return payees;
    }

    public static async void MakePaymentAsync(Account account, double amount, string payee)
    {
        Cache.Accounts = null;
        Cache.SpeficAccountEvents.Remove(account.ID);

        string query = 
            """
                UPDATE Accounts 
                SET 
                    Balance = Balance - @Value, 
                    Change = @Value * -1 
                WHERE 
                    Id = @Id;
            """;

        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", account.ID);
                command.Parameters.AddWithValue("@Value", amount);

                await command.ExecuteNonQueryAsync();
            }

            await connection.CloseAsync();
        }

        AccountEvent paymentEvent = AccountEvent.PaymentEvent(account, payee, amount, NetWorth);
        AddEventAsync(account, paymentEvent);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async void AddInterestAsync(Account account, double interestAmount)
    {
        Cache.Accounts = null;
        Cache.SpeficAccountEvents.Remove(account.ID);

        string query = 
            """
                UPDATE Accounts 
                SET 
                    Balance = Balance + @Amount, 
                    Change = @Amount 
                WHERE 
                    Id = @Id;
            """;

        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", account.ID);
                command.Parameters.AddWithValue("@Amount", interestAmount);
                await command.ExecuteNonQueryAsync();
            }

            await connection.CloseAsync();
        }

        AccountEvent interestEvent = AccountEvent.InterestEvent(account, interestAmount, NetWorth);
        AddEventAsync(account, interestEvent);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async void UpdateValueAsync(Account account, double value)
    {
        Cache.Accounts = null;
        Cache.SpeficAccountEvents.Remove(account.ID);
        
        string query = 
            """ 
                UPDATE Accounts 
                SET 
                    Balance = @Value, 
                    Change = @Change 
                WHERE 
                    Id = @Id;
            """;

        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Value", value);
                command.Parameters.AddWithValue("@Id", account.ID);
                command.Parameters.AddWithValue("@Change", value - account.Balance);
                await command.ExecuteNonQueryAsync();
            }

            await connection.CloseAsync();
        }

        AccountEvent updateEvent = AccountEvent.UpdateValueEvent(account, value, NetWorth);
        AddEventAsync(account, updateEvent);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async Task<Dictionary<char, double>> GetTimeChangesAsync(Account account)
    {
        string query =
            """
                SELECT DATEDIFF(DAY, TimeStamp, GETDATE()) as DaysOld, NewBalance
                FROM Account_Events
                WHERE 
                    PrimaryAccountId = @PrimaryAccountId
                    AND TimeStamp > DATEADD(DAY, -365, GETDATE())
                ORDER BY TimeStamp DESC;
            """;

        Dictionary<char, double> timeChanges = [];
        
        double weeklyStartBalance = -1;
        double monthlyStartBalance = -1;
        double quarterlyStartBalance = -1;
        double yearlyStartBalance = -1;

        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    int daysAgo = reader.GetInt32(0);
                    double oldBalance = reader.GetDouble(1);


                    if (daysAgo < 7)
                    {
                        weeklyStartBalance = oldBalance;
                    }

                    if (daysAgo < 31)
                    {
                        monthlyStartBalance = oldBalance;
                    }

                    if (daysAgo < 93)
                    {
                        quarterlyStartBalance = oldBalance;
                    }

                    if (daysAgo < 365)
                    {
                        yearlyStartBalance = oldBalance;
                    }
                }
            }

            await connection.CloseAsync();
        }

        timeChanges['w'] = weeklyStartBalance == -1 ? 0 : account.Balance - weeklyStartBalance;
        timeChanges['m'] = monthlyStartBalance == -1 ? 0 : account.Balance - monthlyStartBalance;
        timeChanges['q'] = quarterlyStartBalance == -1 ? 0 : account.Balance - quarterlyStartBalance;
        timeChanges['y'] = yearlyStartBalance == -1 ? 0 : account.Balance - yearlyStartBalance;

        return timeChanges;
    }

    public static async Task<Dictionary<char, double>> GetTimeChangesAsync()
    {
        string query =
            """
                SELECT DATEDIFF(DAY, TimeStamp, GETDATE()) as DaysOld, NewBalance
                FROM Account_Events
                WHERE 
                    TimeStamp > DATEADD(DAY, -365, GETDATE())
                ORDER BY TimeStamp DESC;
            """;

        Dictionary<char, double> timeChanges = [];

        double weeklyStartBalance = -1;
        double monthlyStartBalance = -1;
        double quarterlyStartBalance = -1;
        double yearlyStartBalance = -1;

        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    int daysAgo = reader.GetInt32(0);
                    double oldBalance = reader.GetDouble(1);


                    if (daysAgo < 7)
                    { 
                        weeklyStartBalance = oldBalance;
                    }

                    if (daysAgo < 31)
                    {
                        monthlyStartBalance = oldBalance;
                    }

                    if (daysAgo < 93)
                    {
                        quarterlyStartBalance = oldBalance;
                    }

                    if (daysAgo < 365)
                    {
                        yearlyStartBalance = oldBalance;
                    }
                }
            }

            await connection.CloseAsync();
        }


        double netWorth = NetWorth;

        timeChanges['w'] = weeklyStartBalance == -1 ? 0 : netWorth - weeklyStartBalance;
        timeChanges['m'] = monthlyStartBalance == -1 ? 0 : netWorth - monthlyStartBalance;
        timeChanges['q'] = quarterlyStartBalance == -1 ? 0 : netWorth - quarterlyStartBalance;
        timeChanges['y'] = yearlyStartBalance == -1 ? 0 : netWorth - yearlyStartBalance;

        return timeChanges;
    }

    public static async Task<DateTime?> GetLastUpdateAsync(Account account)
    {
        string query =
            """
                SELECT TOP 1 TimeStamp
                FROM Account_Events 
                WHERE PrimaryAccountId = @PrimaryAccountId
                ORDER BY TimeStamp DESC;
            """;

        DateTime? lastUpdate;

        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
                
                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                lastUpdate = reader.Read() ?  reader.GetDateTime(0) :  null;
            }

            await connection.CloseAsync();
        }

        return lastUpdate;
    }

    public static async Task<(string payee, double amount)> GetHighestPayeeAsync(Account account)
    {

        string query =
            """
                SELECT SecondaryText, SUM([Value]) AS total_paid
                FROM dbo.Account_Events
                WHERE [Type] = @Type
                AND PrimaryAccountId = @PrimaryAccountId
                GROUP BY SecondaryText
                ORDER BY total_paid DESC;
            """;

        string payee = "None";
        double amount = 0;

        using (NpgsqlConnection connection = new NpgsqlConnection(DBConfig.ConnectionString))
        {
            await connection.OpenAsync();
            
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Type", (int) AccountEventType.Payment);
                command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);

                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                if (reader.Read())
                {
                    amount = reader.GetDouble(1);
                    payee = reader.GetString(0);
                }
            }

            await connection.CloseAsync();
        }

        return new(payee, amount);
    }

    public static class Cache
    {
        public static List<Account>? Accounts { get; set; } = null;
        public static Dictionary<int, List<AccountEvent>> SpeficAccountEvents { get; set; } = [];
        public static List<AccountEvent>? AllAccountEvents { get; set; } = null;
    }
}

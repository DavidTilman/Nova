using Microsoft.Data.SqlClient;

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Nova.Database;
public static class AccountManager
{
    private static readonly SqlConnection connection = new SqlConnection(DBConfig.ConnectionString);
    
    public static event EventHandler? AccountChanged;
    public static double NetWorth => GetAccounts().Sum(a => a.Balance);
    public static string FormattedNetWorth => NetWorth.ToString("C");

    public static async Task ConnectAsync()
    {
        Debug.WriteLine("Attempting to connect to database...");

        if (connection.State == System.Data.ConnectionState.Open)
            return;

        await connection.OpenAsync();

        Debug.WriteLine("Connected to database successfully.");
    }

    public static void Disconnect()
    {
        Debug.WriteLine("Disconnecting from database...");

        if (connection.State == System.Data.ConnectionState.Closed)
            return;
        connection.Close();

        Debug.WriteLine("Disconnected from database.");
    }

    public static async void AddAccount(Account account)
    {
        Debug.WriteLine($"Adding account: {account.AccountName} ({account.AccountType}) with balance {account.Balance:C}");

        Cache.Accounts = null;
        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");
        
        string query = 
            """
                INSERT INTO Accounts (Name, Provider, Type, Balance, DateCreated, Change) 
                VALUES (@Name, @Provider, @Type, @Balance, @DateCreated, 0); 
                
                SELECT SCOPE_IDENTITY();
            """;

        int newAccountId;
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Name", account.AccountName);
            command.Parameters.AddWithValue("@Provider", account.AccountProvider);
            command.Parameters.AddWithValue("@Type", (byte) account.AccountType);
            command.Parameters.AddWithValue("@Balance", account.Balance);
            command.Parameters.AddWithValue("@DateCreated", DateTime.UtcNow);
            object? result = await command.ExecuteScalarAsync();
            newAccountId = Convert.ToInt32(result);
        }

        Debug.WriteLine($"Updated dbo.Accounts");
        Debug.WriteLine($"New account ID: {newAccountId}");

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

    public static List<Account> GetAccounts()
    {
        Debug.WriteLine("Retrieving accounts from database...");

        if (Cache.Accounts != null)
        {
            Debug.WriteLine("Returning cached accounts.");
            return Cache.Accounts;
        }

        string query = 
            """
                SELECT * 
                FROM Accounts;
            """;

        List<Account> accounts = [];

        using (SqlCommand command = new SqlCommand(query, connection))
        using (SqlDataReader reader = command.ExecuteReader())
        while (reader.Read())
        {
            int id = reader.GetInt32(0);
            string accountName = reader.GetString(1);
            string accountProvider = reader.GetString(2);
            AccountType accountType = (AccountType) reader.GetInt32(3);
            double balance = reader.GetDouble(4);
            DateTime created = reader.GetDateTime(5);
            double change = reader.GetDouble(6);

            Account acc = new Account
            {
                ID = id,
                AccountName = accountName,
                AccountProvider = accountProvider,
                AccountType = accountType,
                Balance = balance,
                DateCreated = created,
                Change = change
            };

            accounts.Add(acc);

            Debug.WriteLine($"Retrieved account: {acc.AccountName} ({acc.AccountType}) with balance {acc.Balance:C}");
        }

        Cache.Accounts = accounts;
        return accounts;
    }

    private static async void AddEventAsync(Account account, AccountEvent accountEvent)
    {
        Debug.WriteLine($"Adding event for account {account.AccountName} ({account.ID}): {accountEvent.EventType} with value {accountEvent.Value:C}");

        Cache.Accounts = null;
        Cache.AccountEvent.Remove(account.ID);

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        string query = 
            """
                INSERT INTO AccountEvents (PrimaryAccountId, Type, Value, SecondaryText, TimeStamp, NewBalance, OldBalance, NetWorth) 
                VALUES (@PrimaryAccountId, @Type, @Value, @SecondaryText, @TimeStamp, @NewBalance, @OldBalance, @NetWorth);
            """;

        using SqlCommand command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
        command.Parameters.AddWithValue("@Type", accountEvent.EventType);
        command.Parameters.AddWithValue("@Value", (object?) accountEvent.Value ?? DBNull.Value);
        command.Parameters.AddWithValue("@SecondaryText", (object?) accountEvent.SecondaryAccountName ?? DBNull.Value);
        command.Parameters.AddWithValue("@TimeStamp", accountEvent.TimeStamp);
        command.Parameters.AddWithValue("@NewBalance", accountEvent.NewBalance);
        command.Parameters.AddWithValue("@OldBalance", accountEvent.OldBalance);
        command.Parameters.AddWithValue("@NetWorth", accountEvent.NetWorth);
        await command.ExecuteNonQueryAsync();

        Debug.WriteLine("Event added successfully.");
    }

    public static async Task<List<AccountEvent>> GetAccountEventsAsync(Account account)
    {
        Debug.WriteLine($"Retrieving events for account {account.AccountName} ({account.ID})...");

        if (Cache.AccountEvent.TryGetValue(account.ID, out List<AccountEvent>? events))
        {
            Debug.WriteLine("Returning cached account events.");
            return events;
        }

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        string query =
            """
                SELECT * 
                FROM AccountEvents 
                WHERE 
                    PrimaryAccountId = @PrimaryAccountId 
                ORDER BY 
                    TimeStamp DESC;
            """;

        List<AccountEvent> accountEvents = [];

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                AccountEventType eventType = (AccountEventType) reader.GetInt32(2);
                double? value = reader.IsDBNull(3) ? null : reader.GetDouble(3);
                string? secondaryAccountName = reader.IsDBNull(4) ? null : reader.GetString(4);
                DateTime timeStamp = reader.GetDateTime(5);
                double newBalance = reader.GetDouble(6);
                double oldBalance = reader.GetDouble(7);
                double netWorth = reader.GetDouble(8);
                AccountEvent accountEvent = new AccountEvent
                {
                    EventType = eventType,
                    Value = value,
                    SecondaryAccountName = secondaryAccountName,
                    TimeStamp = timeStamp,
                    NewBalance = newBalance,
                    OldBalance = oldBalance,
                    NetWorth = netWorth,
                };
                accountEvents.Add(accountEvent);

                Debug.WriteLine($"Retrieved event: {accountEvent.EventType} with value {accountEvent.Value:C} at {accountEvent.TimeStamp}");
            }
        }

        Cache.AccountEvent.Add(account.ID, accountEvents);
        return accountEvents;
    }

    public static async void AddIncomeAsync(Account account, double value, string source)
    {
        Debug.WriteLine($"Adding income to account {account.AccountName} ({account.ID}): {value:C} from {source}");

        string query = 
            """
                UPDATE Accounts 
                SET 
                    Balance = Balance + @Value, 
                    Change = @Value 
                WHERE 
                    Id = @Id;
            """;

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        Cache.Accounts = null;
        Cache.AccountEvents?.Clear();
        Cache.AccountEvent.Remove(account.ID);

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Value", value);
            command.Parameters.AddWithValue("@Id", account.ID);
            await command.ExecuteNonQueryAsync();

            Debug.WriteLine($"Updated account {account.AccountName} ({account.ID}) with new balance {account.Balance + value:C}."); 
        }

        AccountEvent incomeEvent = new AccountEvent
        {
            EventType = AccountEventType.Income,
            Value = value,
            SecondaryAccountName = source,
            TimeStamp = DateTime.UtcNow,
            NewBalance = account.Balance + value,
            OldBalance = account.Balance,
            NetWorth = NetWorth
        };

        AddEventAsync(account, incomeEvent);
        
        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static Account GetAccount(int id)
    {
        if (Cache.Accounts == null)
            GetAccounts();
     
        Account? account = (Cache.Accounts?.FirstOrDefault(a => a.ID == id)) ?? throw new KeyNotFoundException($"Account with ID {id} not found.");
        
        return account;
    }

    public static async void MakeTransferAsync(Account accountTo, Account accountFrom, double value)
    {
        Debug.WriteLine($"Transferring {value:C} from {accountFrom.AccountName} ({accountFrom.ID}) to {accountTo.AccountName} ({accountTo.ID})");

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

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        Cache.Accounts = null;
        Cache.AccountEvents?.Clear();
        Cache.AccountEvent.Remove(accountTo.ID);
        Cache.AccountEvent.Remove(accountFrom.ID);

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Value", value);
            command.Parameters.AddWithValue("@ToId", accountTo.ID);
            command.Parameters.AddWithValue("@FromId", accountFrom.ID);
            await command.ExecuteNonQueryAsync();
            Debug.WriteLine($"Updated accounts: {accountTo.AccountName} new balance {accountTo.Balance + value:C}, {accountFrom.AccountName} new balance {accountFrom.Balance - value:C}");
        }

        AccountEvent transferEventFrom = new AccountEvent
        {
            EventType = AccountEventType.Transfer,
            Value = value,
            SecondaryAccountName = accountFrom.AccountName,
            TimeStamp = DateTime.UtcNow,
            NewBalance = accountTo.Balance + value,
            OldBalance = accountTo.Balance,
            NetWorth = NetWorth
        };

        AddEventAsync(accountTo, transferEventFrom);

        AccountEvent transferEventTo = new AccountEvent
        {
            EventType = AccountEventType.Transfer,
            Value = -value,
            SecondaryAccountName = accountTo.AccountName,
            TimeStamp = DateTime.UtcNow,
            NewBalance = accountFrom.Balance - value,
            OldBalance = accountFrom.Balance,
            NetWorth = NetWorth
        };

        AddEventAsync(accountFrom, transferEventTo);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async Task<List<AccountEvent>> GetAccountEvents(int n)
    {
        Debug.WriteLine($"Retrieving the last {n} account events...");

        if (Cache.AccountEvents != null && Cache.AccountEvents.Count >= n)
        {
            Debug.WriteLine("Returning cached account events.");
            return Cache.AccountEvents.Take(n).ToList();
        }

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        string query = 
            """
                SELECT TOP (@N) * 
                FROM AccountEvents 
                ORDER BY TimeStamp DESC;
            """;

        List<AccountEvent> accountEvents = [];

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@N", n);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
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
                    AccountName = GetAccount(accountId).AccountName,
                    EventType = eventType,
                    Value = value,
                    SecondaryAccountName = secondaryAccountName,
                    TimeStamp = timeStamp,
                    NewBalance = newBalance,
                    OldBalance = oldbalance,
                    NetWorth = netWorth
                };

                accountEvents.Add(accountEvent);

                Debug.WriteLine($"Retrieved event: {accountEvent.EventType} with value {accountEvent.Value:C} at {accountEvent.TimeStamp}");
            }
        }

        return accountEvents;
    }

    public static async Task<List<AccountEvent>> GetAllAccountEventsAsync()
    {
        Debug.WriteLine("Retrieving all account events...");

        if (Cache.AccountEvents != null)
        {
            Debug.WriteLine("Returning cached account events.");
            return [.. Cache.AccountEvents];
        }

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        string query =
            """
                SELECT * 
                FROM AccountEvents 
                ORDER BY TimeStamp DESC;
            """;

        List<AccountEvent> accountEvents = [];

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            using SqlDataReader reader = await command.ExecuteReaderAsync();
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
                    AccountName = GetAccount(accountId).AccountName,
                    EventType = eventType,
                    Value = value,
                    SecondaryAccountName = secondaryAccountName,
                    TimeStamp = timeStamp,
                    NewBalance = newBalance,
                    OldBalance = oldbalance,
                    NetWorth = netWorth
                };

                accountEvents.Add(accountEvent);

                Debug.WriteLine($"Retrieved event: {accountEvent.EventType} with value {accountEvent.Value:C} at {accountEvent.TimeStamp}");
            }
        }

        return accountEvents;
    }

    public static async Task<List<string>> GetPayeesAsync(Account account)
    {
        Debug.WriteLine($"Retrieving payees for account {account.AccountName} ({account.ID})...");

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        string query =
            """
                SELECT DISTINCT SecondaryText 
                FROM AccountEvents 
                WHERE 
                    PrimaryAccountId = @PrimaryAccountId
                    AND 
                    Type = @Type;
            """;

        List<string> payees = [];

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
            command.Parameters.AddWithValue("@Type", (int) AccountEventType.Payment);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                string payee = reader.GetString(0);
                payees.Add(payee);
                Debug.WriteLine($"Found payee: {payee} for account {account.AccountName} ({account.ID})");
            }
        }

        return payees;
    }

    public static async void MakePaymentAsync(Account account, double amount, string payee)
    {
        Debug.WriteLine($"Making payment from account {account.AccountName} ({account.ID}): {amount:C} to {payee}");

        string query = 
            """
                UPDATE Accounts 
                SET 
                    Balance = Balance - @Value, 
                    Change = @Value * -1 
                WHERE 
                    Id = @Id;
            """;

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        Cache.Accounts = null;
        Cache.AccountEvent.Remove(account.ID);

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Id", account.ID);
            command.Parameters.AddWithValue("@Value", amount);
            await command.ExecuteNonQueryAsync();
            Debug.WriteLine($"Updated account {account.AccountName} ({account.ID}) with new balance {account.Balance - amount:C}.");
        }

        AccountEvent payment = new()
        {
            EventType = AccountEventType.Payment,
            Value = amount,
            SecondaryAccountName = payee,
            TimeStamp = DateTime.UtcNow,
            NewBalance = account.Balance - amount,
            OldBalance = account.Balance,
            NetWorth = NetWorth
        };

        AddEventAsync(account, payment);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async void AddInterestAsync(Account account, double interestAmount)
    {
        Debug.WriteLine($"Adding interest to account {account.AccountName} ({account.ID}): {interestAmount:C}");

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        Cache.Accounts = null;
        Cache.AccountEvent.Remove(account.ID);

        string query = 
            """
                UPDATE Accounts 
                SET 
                    Balance = Balance + @Amount, 
                    Change = @Amount 
                WHERE 
                    Id = @Id;
            """;

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Id", account.ID);
            command.Parameters.AddWithValue("@Amount", interestAmount);
            await command.ExecuteNonQueryAsync();
            Debug.WriteLine($"Updated account {account.AccountName} ({account.ID}) with new balance {account.Balance + interestAmount:C}.");
        }

        AccountEvent interest = new()
        {
            EventType = AccountEventType.Interest,
            Value = interestAmount,
            SecondaryAccountName = "Interest",
            TimeStamp = DateTime.UtcNow,
            NewBalance = account.Balance + interestAmount,
            OldBalance = account.Balance,
            NetWorth = NetWorth
        };

        AddEventAsync(account, interest);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async void UpdateValueAsync(Account account, double value)
    {
        Debug.WriteLine($"Updating value of account {account.AccountName} ({account.ID}) to {value:C}");

        string query = 
            """ 
                UPDATE Accounts 
                SET 
                    Balance = @Value, 
                    Change = @Change 
                WHERE 
                    Id = @Id;
            """;

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        Cache.Accounts = null;
        Cache.AccountEvent.Remove(account.ID);

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Value", value);
            command.Parameters.AddWithValue("@Id", account.ID);
            command.Parameters.AddWithValue("@Change", value - account.Balance);
            await command.ExecuteNonQueryAsync();
            Debug.WriteLine($"Updated account {account.AccountName} ({account.ID}) with new balance {value:C}.");
        }

        AccountEvent updateEvent = new AccountEvent
        {
            EventType = AccountEventType.UpdateValue,
            Value = value,
            SecondaryAccountName = "Update",
            TimeStamp = DateTime.UtcNow,
            NewBalance = value,
            OldBalance = account.Balance,
            NetWorth = NetWorth
        };

        AddEventAsync(account, updateEvent);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async Task<Dictionary<char, double>> GetTimeChangesAsync(Account account)
    {
        Debug.WriteLine($"Calculating time changes for account {account.AccountName} ({account.ID})...");

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        string query =
            """
                SELECT DATEDIFF(DAY, TimeStamp, GETDATE()) as DaysOld, NewBalance
                FROM AccountEvents
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

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                int daysAgo = reader.GetInt32(0);
                double oldBalance = reader.GetDouble(1);

                Debug.WriteLine($"Days old: {daysAgo}, Old balance: {oldBalance:C}");

                if (daysAgo < 7)
                {
                    Debug.WriteLine($"Setting weekly start balance to {oldBalance:C}");
                    weeklyStartBalance = oldBalance;
                }

                if (daysAgo < 31)
                {
                    Debug.WriteLine($"Setting monthly start balance to {oldBalance:C}");
                    monthlyStartBalance = oldBalance;
                }

                if (daysAgo < 93)
                {
                    Debug.WriteLine($"Setting quarterly start balance to {oldBalance:C}");
                    quarterlyStartBalance = oldBalance;
                }

                if (daysAgo < 365)
                {
                    Debug.WriteLine($"Setting yearly start balance to {oldBalance:C}");
                    yearlyStartBalance = oldBalance;
                }
            }
        }

        timeChanges['w'] = weeklyStartBalance == -1 ? 0 : account.Balance - weeklyStartBalance;
        timeChanges['m'] = monthlyStartBalance == -1 ? 0 : account.Balance - monthlyStartBalance;
        timeChanges['q'] = quarterlyStartBalance == -1 ? 0 : account.Balance - quarterlyStartBalance;
        timeChanges['y'] = yearlyStartBalance == -1 ? 0 : account.Balance - yearlyStartBalance;

        Debug.WriteLine($"Time changes for account {account.AccountName} ({account.ID}): " +
                          $"Weekly: {timeChanges['w']:C}, " +
                          $"Monthly: {timeChanges['m']:C}, " +
                          $"Quarterly: {timeChanges['q']:C}, " +
                          $"Yearly: {timeChanges['y']:C}");

        return timeChanges;
    }

    public static async Task<Dictionary<char, double>> GetTimeChangesAsync()
    {
        Debug.WriteLine("Calculating time changes for all accounts...");

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        string query =
            """
                SELECT DATEDIFF(DAY, TimeStamp, GETDATE()) as DaysOld, NewBalance
                FROM AccountEvents
                WHERE 
                    TimeStamp > DATEADD(DAY, -365, GETDATE())
                ORDER BY TimeStamp DESC;
            """;

        Dictionary<char, double> timeChanges = [];

        double weeklyStartBalance = -1;
        double monthlyStartBalance = -1;
        double quarterlyStartBalance = -1;
        double yearlyStartBalance = -1;

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                int daysAgo = reader.GetInt32(0);
                double oldBalance = reader.GetDouble(1);

                Debug.WriteLine($"Days old: {daysAgo}, Old balance: {oldBalance:C}");

                if (daysAgo < 7)
                {
                    Debug.WriteLine($"Setting weekly start balance to {oldBalance:C}");
                    weeklyStartBalance = oldBalance;
                }

                if (daysAgo < 31)
                {
                    Debug.WriteLine($"Setting monthly start balance to {oldBalance:C}");
                    monthlyStartBalance = oldBalance;
                }

                if (daysAgo < 93)
                {
                    Debug.WriteLine($"Setting quarterly start balance to {oldBalance:C}");
                    quarterlyStartBalance = oldBalance;
                }

                if (daysAgo < 365)
                {
                    Debug.WriteLine($"Setting yearly start balance to {oldBalance:C}");
                    yearlyStartBalance = oldBalance;
                }
            }
        }

        double netWorth = NetWorth;

        timeChanges['w'] = weeklyStartBalance == -1 ? 0 : netWorth - weeklyStartBalance;
        timeChanges['m'] = monthlyStartBalance == -1 ? 0 : netWorth - monthlyStartBalance;
        timeChanges['q'] = quarterlyStartBalance == -1 ? 0 : netWorth - quarterlyStartBalance;
        timeChanges['y'] = yearlyStartBalance == -1 ? 0 : netWorth - yearlyStartBalance;

        Debug.WriteLine($"Time changes for all accounts: " +
                          $"Weekly: {timeChanges['w']:C}, " +
                          $"Monthly: {timeChanges['m']:C}, " +
                          $"Quarterly: {timeChanges['q']:C}, " +
                          $"Yearly: {timeChanges['y']:C}");

        return timeChanges;
    }

    public static async Task<DateTime?> GetLastUpdateAsync(Account account)
    {
        Debug.WriteLine($"Retrieving last update time for account {account.AccountName} ({account.ID})...");

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        string query =
            """
                SELECT TOP 1 TimeStamp
                FROM AccountEvents 
                WHERE PrimaryAccountId = @PrimaryAccountId
                ORDER BY TimeStamp DESC;
            """;

        using SqlCommand command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);

        using SqlDataReader reader = await command.ExecuteReaderAsync();

        DateTime? lastUpdate = reader.Read() ?  reader.GetDateTime(0) :  null;

        Debug.WriteLine($"Last update for account {account.AccountName} ({account.ID}): {lastUpdate?.ToString("o") ?? "Never"}");

        return lastUpdate;
    }

    public static async Task<Tuple<string, double>> GetHighestPayeeAsync(Account account)
    {
        Debug.WriteLine($"Retrieving highest payee for account {account.AccountName} ({account.ID})...");

        string query =
            """
                SELECT SecondaryText, SUM([Value]) AS total_paid
                FROM dbo.AccountEvents
                WHERE [Type] = @Type
                AND PrimaryAccountId = @PrimaryAccountId
                GROUP BY SecondaryText
                ORDER BY total_paid DESC;
            """;

        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        string payee = "None";
        double amount = 0;

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Type", (int) AccountEventType.Payment);
            command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (reader.Read())
            {
                amount = reader.GetDouble(1);
                payee = reader.GetString(0);
            }
        }

        Debug.WriteLine($"Highest payee for account {account.AccountName} ({account.ID}): {payee} with total paid {amount:C}.");

        return new(payee, amount);
    }

    public static class Cache
    {
        public static List<Account>? Accounts { get; set; } = null;
        public static Dictionary<int, List<AccountEvent>> AccountEvent { get; set; } = [];
        public static List<AccountEvent>? AccountEvents { get; set; } = null;
    }
}

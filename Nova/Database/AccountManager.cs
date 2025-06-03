using Microsoft.Data.SqlClient;

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Nova.Database;

public static class AccountManager
{
    private static SqlConnection connection = new SqlConnection(Config.ConnectionString);

    public static async Task ConnectAsync()
    {
        if (connection.State == System.Data.ConnectionState.Open)
            return;

        await connection.OpenAsync();
    }

    public static void Disconnect()
    {
        if (connection.State == System.Data.ConnectionState.Closed)
            return;
        connection.Close();
    }

    public static void AddAccount(Account account)
    {
        AccountEvent creation = new AccountEvent
        {
            EventType = AccountEventType.Created,
            NewBalance = account.Balance,
            OldBalance = 0,
            TimeStamp = account.DateCreated
        };
        Cache.Accounts = null; // Clear cache to ensure fresh data is fetched next time
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
            object? result = command.ExecuteScalar();
            newAccountId = Convert.ToInt32(result);
        }

        account.ID = newAccountId;

        AddEvent(account, creation);
    }

    public static List<Account> GetAccounts()
    {
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

        List<Account> accounts = new List<Account>();
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
        }
        Cache.Accounts = accounts;
        return accounts;
    }

    public static double NetWorth => GetAccounts().Sum(a => a.Balance);
    public static string FormattedNetWorth => NetWorth.ToString("C");


    private static void AddEvent(Account account, AccountEvent accountEvent)
    {
        Debug.WriteLine($"{account.ID}");
        Cache.Accounts = null;
        Cache.AccountEvent.Remove(account.ID);
        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");
        string query = 
            """
                INSERT INTO AccountEvents (PrimaryAccountId, Type, Value, SecondaryText, TimeStamp, NewBalance, OldBalance) 
                VALUES (@PrimaryAccountId, @Type, @Value, @SecondaryText, @TimeStamp, @NewBalance, @OldBalance);
            """;

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
            command.Parameters.AddWithValue("@Type", accountEvent.EventType);
            command.Parameters.AddWithValue("@Value", (object?)accountEvent.Value ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryText", (object?)accountEvent.SecondaryAccountName ?? DBNull.Value);
            command.Parameters.AddWithValue("@TimeStamp", accountEvent.TimeStamp);
            command.Parameters.AddWithValue("@NewBalance", accountEvent.NewBalance);
            command.Parameters.AddWithValue("@OldBalance", accountEvent.OldBalance);
            command.ExecuteNonQuery();
        }
    }

    public static List<AccountEvent> GetAccountEvents(Account account)
    {
        if (Cache.AccountEvent.TryGetValue(account.ID, out List<AccountEvent>? events))
        {
            Debug.WriteLine("Returning cached account events.");
            return events;
        }
        string query =
            """
                SELECT * 
                FROM AccountEvents 
                WHERE 
                    PrimaryAccountId = @PrimaryAccountId 
                ORDER BY 
                    TimeStamp DESC;
            """;
        List<AccountEvent> accountEvents = new List<AccountEvent>();
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    AccountEventType eventType = (AccountEventType) reader.GetInt32(2);
                    double? value = reader.IsDBNull(3) ? null : reader.GetDouble(3);
                    string? secondaryAccountName = reader.IsDBNull(4) ? null : reader.GetString(4);
                    DateTime timeStamp = reader.GetDateTime(5);
                    double newBalance = reader.GetDouble(6);
                    double oldBalance = reader.GetDouble(7);
                    AccountEvent accountEvent = new AccountEvent
                    {
                        EventType = eventType,
                        Value = value,
                        SecondaryAccountName = secondaryAccountName,
                        TimeStamp = timeStamp,
                        NewBalance = newBalance,
                        OldBalance = oldBalance,
                    };
                    accountEvents.Add(accountEvent);
                }
            }
        }
        Cache.AccountEvent.Add(account.ID, accountEvents);
        return accountEvents;
    }

    public static event EventHandler? AccountChanged;

    public static void AddIncome(Account account, double value, string source)
    {
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
            command.ExecuteNonQuery();
        }

        AccountEvent incomeEvent = new AccountEvent
        {
            EventType = AccountEventType.Income,
            Value = value,
            SecondaryAccountName = source,
            TimeStamp = DateTime.UtcNow,
            NewBalance = account.Balance + value,
            OldBalance = account.Balance
        };
        AddEvent(account, incomeEvent);
        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static Account GetAccount(int id)
    {
        if (Cache.Accounts == null)
            GetAccounts();
        Account? account = Cache.Accounts?.FirstOrDefault(a => a.ID == id);
        if (account == null)
            throw new KeyNotFoundException($"Account with ID {id} not found.");
        return account;
    }

    public static void MakeTransfer(Account accountTo, Account accountFrom, double value)
    {
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
            command.ExecuteNonQuery();
        }
        AccountEvent transferEventFrom = new AccountEvent
        {
            EventType = AccountEventType.Transfer,
            Value = value,
            SecondaryAccountName = accountFrom.AccountName,
            TimeStamp = DateTime.UtcNow,
            NewBalance = accountTo.Balance + value,
            OldBalance = accountTo.Balance
        };
        AddEvent(accountTo, transferEventFrom);
        AccountEvent transferEventTo = new AccountEvent
        {
            EventType = AccountEventType.Transfer,
            Value = -value,
            SecondaryAccountName = accountTo.AccountName,
            TimeStamp = DateTime.UtcNow,
            NewBalance = accountFrom.Balance - value,
            OldBalance = accountFrom.Balance
        };
        AddEvent(accountFrom, transferEventTo);
        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static List<AccountEvent> GetAccountEvents(int n)
    {
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
        List<AccountEvent> accountEvents = new List<AccountEvent>();
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@N", n);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int accountId = reader.GetInt32(1);
                    AccountEventType eventType = (AccountEventType) reader.GetInt32(2);
                    double? value = reader.IsDBNull(3) ? null : reader.GetDouble(3);
                    string? secondaryAccountName = reader.IsDBNull(4) ? null : reader.GetString(4);
                    DateTime timeStamp = reader.GetDateTime(5);
                    double newBalance = reader.GetDouble(6);
                    double oldbalance = reader.GetDouble(7);
                    AccountEvent accountEvent = new AccountEvent
                    {
                        AccountName = GetAccount(accountId).AccountName,
                        EventType = eventType,
                        Value = value,
                        SecondaryAccountName = secondaryAccountName,
                        TimeStamp = timeStamp,
                        NewBalance = newBalance,
                        OldBalance = oldbalance
                    };
                    accountEvents.Add(accountEvent);
                }
            }
        }
        return accountEvents;
    }

    public static List<string> GetPayees(Account account)
    {
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
        List<string> payees = new List<string>();
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
            command.Parameters.AddWithValue("@Type", (int) AccountEventType.Payment);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string payee = reader.GetString(0);
                    payees.Add(payee);
                }
            }
        }
        return payees;
    }

    public static void MakePayment(Account account, double amount, string payee)
    {
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
            command.ExecuteNonQuery();
        }

        AddEvent(account, new AccountEvent
        {
            EventType = AccountEventType.Payment,
            Value = amount,
            SecondaryAccountName = payee,
            TimeStamp = DateTime.UtcNow,
            NewBalance = account.Balance - amount,
            OldBalance = account.Balance
        });

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void AddInterest(Account account, double interestAmount)
    {
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
            command.ExecuteNonQuery();
        }
        AddEvent(account, new AccountEvent
        {
            EventType = AccountEventType.Interest,
            Value = interestAmount,
            SecondaryAccountName = "Interest",
            TimeStamp = DateTime.UtcNow,
            NewBalance = account.Balance + interestAmount,
            OldBalance = account.Balance
        });
        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void UpdateValue(Account account, double value)
    {
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
            command.ExecuteNonQuery();
        }

        AccountEvent updateEvent = new AccountEvent
        {
            EventType = AccountEventType.UpdateValue,
            Value = value,
            SecondaryAccountName = "Update",
            TimeStamp = DateTime.UtcNow,
            NewBalance = value,
            OldBalance = account.Balance
        };
        AddEvent(account, updateEvent);
        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static Dictionary<char, double> GetTimeChanges(Account account)
    {
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
        Dictionary<char, double> timeChanges = new Dictionary<char, double>();
        
        double weeklyStartBalance = -1;
        double monthlyStartBalance = -1;
        double quarterlyStartBalance = -1;
        double yearlyStartBalance = -1;

        DateTime weekCutoff = DateTime.UtcNow.AddDays(-7);
        DateTime monthCutoff = DateTime.UtcNow.AddMonths(-31);
        DateTime quarterCutoff = DateTime.UtcNow.AddMonths(-3);
        DateTime yearCutoff = DateTime.UtcNow.AddYears(-1);

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
            using (SqlDataReader reader = command.ExecuteReader())
            {
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
        }

        timeChanges['w'] = weeklyStartBalance == -1 ? 0 : account.Balance - weeklyStartBalance;
        timeChanges['m'] = monthlyStartBalance == -1 ? 0 : account.Balance - monthlyStartBalance;
        timeChanges['q'] = quarterlyStartBalance == -1 ? 0 : account.Balance - quarterlyStartBalance;
        timeChanges['y'] = yearlyStartBalance == -1 ? 0 : account.Balance - yearlyStartBalance;

        return timeChanges;
    }

    public static DateTime? GetLastUpdate(Account account)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");

        string query =
            """
                SELECT TOP 1 TimeStamp
                FROM AccountEvents 
                WHERE PrimaryAccountId = @PrimaryAccountId
                ORDER BY TimeStamp DESC;
            """;
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return reader.GetDateTime(0);
                }
            }
        }
        return null;
    }

    public static string GetHighestPayee(Account account, out double amount)
    {
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
        amount = 0;
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Type", (int) AccountEventType.Payment);
            command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    amount = reader.GetDouble(1);
                    return reader.GetString(0);
                }
            }
        }
        return "None";
    }

    public static class Cache
    {
        public static List<Account>? Accounts { get; set; } = null;
        public static Dictionary<int, List<AccountEvent>> AccountEvent { get; set; } = [];
        public static List<AccountEvent>? AccountEvents { get; set; } = null;
    }
}

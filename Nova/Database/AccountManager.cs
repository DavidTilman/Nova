using Npgsql;

namespace Nova.Database;
public static class AccountManager
{
    public static event EventHandler? AccountChanged;
    public static double NetWorth => GetAccountsAsync().Result.Sum(a => a.Balance);
    public static string FormattedNetWorth => NetWorth.ToString("C");

    private static readonly string connectionString = Environment.GetEnvironmentVariable("NOVA-DatabaseConnectionString")!;

    public static async Task AddAccountAsync(Account account)
    {
        Cache.Accounts = null;

        string query =
            """
                INSERT INTO accounts (name, provider, type, balance, date_created, change) 
                VALUES (@Name, @Provider, @Type, @Balance, @DateCreated, 0)
                RETURNING id;
            """;

        int newAccountId;

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Name", account.AccountName);
                command.Parameters.AddWithValue("@Provider", account.AccountProvider);
                command.Parameters.AddWithValue("@Type", (byte) account.AccountType);
                command.Parameters.AddWithValue("@Balance", Convert.ToDecimal(account.Balance));
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

        await AddEventAsync(account, creation);
    }

    public static async Task<List<Account>> GetAccountsAsync()
    {
        if (Cache.Accounts != null)
            return Cache.Accounts;

        string query =
            """
                SELECT * 
                FROM accounts
                ORDER BY Balance DESC;
            """;

        List<Account> accounts = [];

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                using NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Account acc = Account.FromReader(reader);

                    accounts.Add(acc);
                }
            }

            await connection.CloseAsync().ConfigureAwait(false);
        }

        Cache.Accounts = accounts;

        return accounts;
    }

    private static async Task AddEventAsync(Account account, AccountEvent accountEvent)
    {
        Cache.Accounts = null;
        Cache.SpeficAccountEvents.Remove(account.ID);
        Cache.AllAccountEvents = null;

        using NpgsqlConnection connection = new NpgsqlConnection(connectionString);

        await connection.OpenAsync().ConfigureAwait(false);
        ;

        using (NpgsqlCommand command = AccountEvent.InsertCommand(account, accountEvent))
        {
            command.Connection = connection;
            await command.ExecuteNonQueryAsync();
        }

        await connection.CloseAsync().ConfigureAwait(false);
    }

    public static async Task<List<AccountEvent>> GetAccountEventsByIdAsync(int accountId)
    {
        if (Cache.SpeficAccountEvents.TryGetValue(accountId, out List<AccountEvent>? cachedEvents))
            return cachedEvents;

        string query =
            """
                SELECT * 
                FROM account_events 
                WHERE 
                    account_id = @PrimaryAccountId 
                ORDER BY 
                    TimeStamp DESC;
            """;

        List<AccountEvent> events = [];

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PrimaryAccountId", accountId);

                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

                while (reader.Read())
                {
                    AccountEvent accountEvent = await AccountEvent.FromReader(reader);
                    events.Add(accountEvent);
                }
            }

            await connection.CloseAsync().ConfigureAwait(false);
        }

        Cache.SpeficAccountEvents.Add(accountId, events);
        return events;
    }

    public static async Task AddIncomeAsync(Account account, double value, string source, DateTime timeStamp)
    {
        if (account.AccountType is not AccountType.None and not AccountType.Current)
        {
            throw new InvalidOperationException("Income can only be added to Current accounts.");
        }

        Cache.Accounts = null;
        Cache.AllAccountEvents?.Clear();
        Cache.SpeficAccountEvents.Remove(account.ID);

        string query =
            """
                UPDATE accounts 
                SET 
                    balance = balance + @Value::money, 
                    change = @Value::money
                WHERE 
                    id = @Id;
            """;

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Value", Convert.ToDecimal(value));
                command.Parameters.AddWithValue("@Id", account.ID);
                await command.ExecuteNonQueryAsync();
            }

            await connection.CloseAsync().ConfigureAwait(false);
        }

        AccountEvent incomeEvent = AccountEvent.IncomeEvent(account, source, value, NetWorth, timeStamp);

        await AddEventAsync(account, incomeEvent);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async Task<Account?> GetAccountAsync(int id)
    {
        if (Cache.Accounts == null)
            await GetAccountsAsync();

        Account? account = Cache.Accounts!.FirstOrDefault(a => a.ID == id);

        return account;
    }

    public static async Task MakeTransferAsync(Account accountTo, Account accountFrom, double value, DateTime timeStamp)
    {
        Cache.Accounts = null;
        Cache.AllAccountEvents?.Clear();
        Cache.SpeficAccountEvents.Remove(accountTo.ID);
        Cache.SpeficAccountEvents.Remove(accountFrom.ID);

        string query =
            """
                UPDATE accounts 
                SET 
                    balance = balance + @Value::money, 
                    change = @Value::money
                WHERE 
                    id = @ToId; 
                
                UPDATE accounts 
                SET 
                    balance = balance - @Value::money, 
                    change = @Value::money* -1 
                WHERE 
                    id = @FromId;
            """;

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Value", Convert.ToDecimal(value));
                command.Parameters.AddWithValue("@ToId", accountTo.ID);
                command.Parameters.AddWithValue("@FromId", accountFrom.ID);
                await command.ExecuteNonQueryAsync();
            }

            await connection.CloseAsync().ConfigureAwait(false);
        }

        (AccountEvent eventTo, AccountEvent eventFrom) transferEvents = AccountEvent.TransferAccountEvents(accountTo, accountFrom, value, NetWorth, timeStamp);

        await AddEventAsync(accountTo, transferEvents.eventTo);

        await AddEventAsync(accountFrom, transferEvents.eventFrom);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async Task<List<AccountEvent>> GetAccountEventsWithLimitAsync(int n)
    {
        if (Cache.AllAccountEvents != null && Cache.AllAccountEvents.Count >= n)
            return Cache.AllAccountEvents.Take(n).ToList();

        string query =
            """
                SELECT * 
                FROM account_events 
                ORDER BY timestamp DESC
                LIMIT @N;
            """;

        List<AccountEvent> Account_Events = [];

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@N", n);

                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

                while (reader.Read())
                {
                    AccountEvent accountEvent = await AccountEvent.FromReader(reader);

                    Account_Events.Add(accountEvent);
                }
            }

            await connection.CloseAsync().ConfigureAwait(false);
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
                FROM account_events 
                ORDER BY timestamp DESC;
            """;

        List<AccountEvent> accountEvents = [];

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

                while (reader.Read())
                {
                    AccountEvent accountEvent = await AccountEvent.FromReader(reader);
                    accountEvents.Add(accountEvent);

                }
            }

            await connection.CloseAsync().ConfigureAwait(false);
        }

        return accountEvents;
    }

    public static async Task<List<string>> GetPayeesAsync(Account account)
    {
        if (account.AccountType is not AccountType.None and not AccountType.Current)
        {
            throw new InvalidOperationException("Payees can only be found from Current accounts.");
        }

        string query =
            """
                SELECT DISTINCT description 
                FROM account_events 
                WHERE 
                    account_id = @PrimaryAccountId
                    AND 
                    type = @Type;
            """;

        List<string> payees = [];

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;

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

            await connection.CloseAsync().ConfigureAwait(false);
        }

        return payees;
    }
    public static async Task<List<string>> GetPayeesAsync(int id)
    {
        string query =
            """
                SELECT DISTINCT description 
                FROM account_events 
                WHERE 
                    account_id = @PrimaryAccountId
                    AND 
                    type = @Type;
            """;

        List<string> payees = [];

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PrimaryAccountId", id);
                command.Parameters.AddWithValue("@Type", (int) AccountEventType.Payment);

                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    string payee = reader.GetString(0);
                    payees.Add(payee);
                }
            }

            await connection.CloseAsync().ConfigureAwait(false);
        }

        return payees;
    }

    public static async Task MakePaymentAsync(Account account, double amount, string payee, DateTime timeStamp)
    {
        if (account.AccountType is not AccountType.None and not AccountType.Current)
        {
            throw new InvalidOperationException("Payments can only be made from Current accounts.");
        }

        Cache.Accounts = null;
        Cache.SpeficAccountEvents.Remove(account.ID);

        string query =
            """
                UPDATE accounts 
                SET 
                    balance = balance - @Value::money, 
                    change = @Value::money * -1 
                WHERE 
                    id = @Id;
            """;

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", account.ID);
                command.Parameters.AddWithValue("@Value", Convert.ToDecimal(amount));

                await command.ExecuteNonQueryAsync();
            }

            await connection.CloseAsync().ConfigureAwait(false);
        }

        AccountEvent paymentEvent = AccountEvent.PaymentEvent(account, payee, amount, NetWorth, timeStamp);
        await AddEventAsync(account, paymentEvent);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async Task AddInterestAsync(Account account, double interestAmount, DateTime timeStamp)
    {
        Cache.Accounts = null;
        Cache.SpeficAccountEvents.Remove(account.ID);

        string query =
            """
                UPDATE accounts 
                SET 
                    balance = balance + @Amount::money, 
                    change = @Amount 
                WHERE 
                    id = @Id;
            """;

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", account.ID);
                command.Parameters.AddWithValue("@Amount", Convert.ToDecimal(interestAmount));
                await command.ExecuteNonQueryAsync();
            }

            await connection.CloseAsync().ConfigureAwait(false);
        }

        AccountEvent interestEvent = AccountEvent.InterestEvent(account, interestAmount, NetWorth, timeStamp);
        await AddEventAsync(account, interestEvent);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async Task UpdateValueAsync(Account account, double value)
    {
        if (account.AccountType is not AccountType.Asset and not AccountType.Investment)
        {
            throw new InvalidOperationException("Only Assets and Investments can have their balance updated.");
        }

        Cache.Accounts = null;
        Cache.SpeficAccountEvents.Remove(account.ID);

        string query =
            """ 
                UPDATE accounts 
                SET 
                    balance = @Value::money, 
                    change = @Change::money
                WHERE 
                    id = @Id;
            """;

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Value", Convert.ToDecimal(value));
                command.Parameters.AddWithValue("@Id", account.ID);
                command.Parameters.AddWithValue("@Change", Convert.ToDecimal(value - account.Balance));
                await command.ExecuteNonQueryAsync();
            }

            await connection.CloseAsync().ConfigureAwait(false);
        }

        AccountEvent updateEvent = AccountEvent.UpdateValueEvent(account, value, NetWorth);
        await AddEventAsync(account, updateEvent);

        AccountChanged?.Invoke(null, EventArgs.Empty);
    }

    public static async Task<Dictionary<char, double>> GetTimeChangesAsync(Account account)
    {
        string query =
            """
                SELECT (CURRENT_DATE - timestamp::date)::int, new_balance
                FROM account_events
                WHERE 
                    timestamp > CURRENT_DATE - INTERVAL '365 days' AND account_id = @PrimaryAccountId
                ORDER BY timestamp DESC;
            """;

        Dictionary<char, double> timeChanges = [];

        double weeklyStartBalance = -1;
        double monthlyStartBalance = -1;
        double quarterlyStartBalance = -1;
        double yearlyStartBalance = -1;

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);
                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    int daysAgo = reader.GetInt32(0);
                    double oldBalance = Convert.ToDouble(reader.GetDecimal(1));

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

            await connection.CloseAsync().ConfigureAwait(false);
        }

        timeChanges['w'] = weeklyStartBalance == -1 ? 0 : account.Balance - weeklyStartBalance;
        timeChanges['m'] = monthlyStartBalance == -1 ? 0 : account.Balance - monthlyStartBalance;
        timeChanges['q'] = quarterlyStartBalance == -1 ? 0 : account.Balance - quarterlyStartBalance;
        timeChanges['y'] = yearlyStartBalance == -1 ? 0 : account.Balance - yearlyStartBalance;

        return timeChanges;
    }

    public static async Task<Dictionary<char, double>> GetTimeChangesAsync()
    {
        string selectAllEvents =
            """
                SELECT (CURRENT_DATE - timestamp::date)::int, new_balance
                FROM account_events
                WHERE 
                    timestamp > CURRENT_DATE - INTERVAL '365 days'
                ORDER BY timestamp DESC;
            """;

        string selectStartingNetworth =
            """
                SELECT
                SUM(new_balance) - (
                    SELECT new_balance
                    FROM account_events
                    WHERE type = 0
                    ORDER BY timestamp ASC
                    LIMIT 1
                )
                FROM account_events
                WHERE
                    timestamp > CURRENT_DATE - INTERVAL '365 days' AND
                    type = 0;
            """;

        Dictionary<char, double> timeChanges = [];

        double weeklyStartBalance = -1;
        double monthlyStartBalance = -1;
        double quarterlyStartBalance = -1;
        double yearlyStartBalance = -1;

        double startingAccountBalances = 0;

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);

            using (NpgsqlCommand command = new NpgsqlCommand(selectAllEvents, connection))
            {
                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                while (reader.Read())
                {
                    int daysAgo = reader.GetInt32(0);
                    double oldBalance = Convert.ToDouble(reader.GetDecimal(1));

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

            using (NpgsqlCommand command = new NpgsqlCommand(selectStartingNetworth, connection))
            {
                object? result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    startingAccountBalances = Convert.ToDouble(result);
                }
            }

            await connection.CloseAsync().ConfigureAwait(false);
        }

        double netWorth = NetWorth;

        timeChanges['w'] = weeklyStartBalance == -1 ? 0 : netWorth - startingAccountBalances - weeklyStartBalance;
        timeChanges['m'] = monthlyStartBalance == -1 ? 0 : netWorth - startingAccountBalances - monthlyStartBalance;
        timeChanges['q'] = quarterlyStartBalance == -1 ? 0 : netWorth - startingAccountBalances - quarterlyStartBalance;
        timeChanges['y'] = yearlyStartBalance == -1 ? 0 : netWorth - startingAccountBalances - yearlyStartBalance;

        return timeChanges;
    }

    public static async Task<DateTime?> GetLastUpdateAsync(int accountId)
    {
        string query =
            """
                SELECT timestamp
                FROM account_events 
                WHERE account_id = @PrimaryAccountId
                ORDER BY timestamp DESC
                LIMIT 1;
            """;

        DateTime? lastUpdate;

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PrimaryAccountId", accountId);

                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                lastUpdate = reader.Read() ? reader.GetDateTime(0) : null;
            }

            await connection.CloseAsync().ConfigureAwait(false);
        }

        return lastUpdate;
    }

    public static async Task<(string payee, double amount)> GetHighestPayeeAsync(Account account)
    {

        string query =
            """
                SELECT description, SUM(value) AS total_paid
                FROM account_events
                WHERE type = @Type
                AND account_id = @PrimaryAccountId
                GROUP BY description
                ORDER BY total_paid DESC;
            """;

        string payee = "None";
        double amount = 0;

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            ;

            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Type", (int) AccountEventType.Payment);
                command.Parameters.AddWithValue("@PrimaryAccountId", account.ID);

                using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
                if (reader.Read())
                {
                    amount = Convert.ToDouble(reader.GetDecimal(1));
                    payee = reader.GetString(0);
                }
            }

            await connection.CloseAsync().ConfigureAwait(false);
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
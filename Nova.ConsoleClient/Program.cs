using Nova.Database;

namespace Nova.ConsoleClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        string command = args.Length > 0 ? args[0].ToLower() : "help";

        switch (command)
        {
            case "accounts":
                await AccountsCommand();
                break;
            case "events":
                await EventsCommand(args.Skip(1).ToArray());
                break;
            case "create":
                await CreateCommand(args.Skip(1).ToArray());
                break;
            case "income":
                await IncomeCommand(args.Skip(1).ToArray());
                break;
            case "transfer":
                await TransferCommand(args.Skip(1).ToArray());
                break;
            case "payees":
                await PayeesCommand(args.Skip(1).ToArray());
                break;
            case "payment":
                await PaymentCommand(args.Skip(1).ToArray());
                break;
            case "interest":
                await InterestCommand(args.Skip(1).ToArray());
                break;
            case "update":
                await UpdateCommand(args.Skip(1).ToArray());
                break;
            case "changes":
                await ChangesCommand(args.Skip(1).ToArray());
                break;
            default:
                Console.WriteLine($"Unknown command: {command}");
                break;
        }

        static async Task CreateCommand(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: create <accountName> <accountProvider> <accountType> <initialBalance>");
                return;
            }

            string accountName = args[0];
            string accountProvider = args[1];
            if (!Enum.TryParse(args[2], true, out AccountType accountType))
            {
                Console.WriteLine("Invalid account type. Valid types are: None, Current, Savings, Investment, Asset.");
                return;
            }

            if (!double.TryParse(args[3], out double initialBalance))
            {
                Console.WriteLine("Invalid initial balance. Please enter a valid number.");
                return;
            }

            double.Round(initialBalance, 2);

            Account account = new Account
            {
                AccountName = accountName,
                AccountProvider = accountProvider,
                AccountType = accountType,
                Balance = initialBalance,
                DateCreated = DateTime.UtcNow,
                Change = 0
            };
            Console.WriteLine($"Create account: {account}\nConfirm? Y/[N]");
            string? confirm = Console.ReadLine()?.Trim().ToLower();
            if (confirm is not "y" and not "yes")
            {
                Console.WriteLine("Account creation cancelled.");
                return;
            }

            try
            {
                await Database.AccountManager.AddAccountAsync(account);
                Console.WriteLine($"Account '{account.AccountName}' created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating account: {ex.Message}");
            }
        }
    }

    private static async Task ChangesCommand(string[] args)
    {
        if (args.Length > 1)
        {
            Console.WriteLine("Usage: changes <opt: accountId>");
            return;
        }

        if (args.Length == 0)
        {
            Dictionary<char, double> timeChanges = await AccountManager.GetTimeChangesAsync();
            foreach (char key in timeChanges.Keys)
            {
                Console.WriteLine($"{char.ToUpper(key)}: {timeChanges[key]:C}");
            }
        }
        else if (args.Length == 1)
        {
            if (!int.TryParse(args[0], out int accountId))
            {
                Console.WriteLine("Invalid account ID. Please enter a valid number.");
                return;
            }

            Account? account = await AccountManager.GetAccountAsync(accountId);
            if (account == null)
            {
                Console.WriteLine($"Account with ID {accountId} not found.");
                return;
            }

            Dictionary<char, double> timeChanges = await AccountManager.GetTimeChangesAsync(account);
            foreach (char key in timeChanges.Keys)
            {
                Console.WriteLine($"{char.ToUpper(key)}: {timeChanges[key]:C}");
            }
        }
    }

    private static async Task UpdateCommand(string[] strings)
    {
        if (strings.Length != 2)
        {
            Console.WriteLine("Usage: update <accountId> <newBalance>");
            return;
        }

        if (!int.TryParse(strings[0], out int accountId))
        {
            Console.WriteLine("Invalid account ID. Please enter a valid number.");
            return;
        }

        if (!double.TryParse(strings[1], out double newBalance))
        {
            Console.WriteLine("Invalid new balance. Please enter a valid number.");
            return;
        }

        Account? account = await Database.AccountManager.GetAccountAsync(accountId);
        if (account == null)
        {
            Console.WriteLine($"Account with ID {accountId} not found.");
            return;
        }

        double.Round(newBalance, 2);
        Console.WriteLine($"Update account: {account.AccountName} to new balance: {newBalance:C}.\nConfirm? Y/[N]");
        string? confirm = Console.ReadLine()?.Trim().ToLower();
        if (confirm is not "y" and not "yes")
        {
            Console.WriteLine("Account update cancelled.");
            return;
        }

        await AccountManager.UpdateValueAsync(account, newBalance).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Console.WriteLine($"Account balance updated successfully.");
            }
            else
            {
                Console.WriteLine($"Error updating account: {task.Exception?.Message}");
            }
        });
    }

    private static async Task InterestCommand(string[] args)
    {
        if (args.Length is not 2 and not 3)
        {
            Console.WriteLine("Usage: interest <accountId> <amount> <opt | daysPrevious | 0>");
            return;
        }

        if (!int.TryParse(args[0], out int accountId))
        {
            Console.WriteLine("Invalid account ID. Please enter a valid number.");
            return;
        }

        if (!double.TryParse(args[1], out double amount))
        {
            Console.WriteLine("Invalid amount. Please enter a valid number.");
            return;
        }

        Account? account = await Database.AccountManager.GetAccountAsync(accountId);
        if (account == null)
        {
            Console.WriteLine($"Account with ID {accountId} not found.");
            return;
        }

        double.Round(amount, 2);

        int dateOffset = 0;
        if (args.Length == 3 && !int.TryParse(args[2], out dateOffset))
        {
            Console.WriteLine("Invalid date offset. Please enter a valid number.");
            return;
        }

        DateTime timeStamp = DateTime.UtcNow.AddDays(-dateOffset);

        Console.WriteLine($"Add interest: {amount:C} to account '{account.AccountName}'.\nConfirm? Y/[N]");
        string? confirm = Console.ReadLine()?.Trim().ToLower();
        if (confirm is not "y" and not "yes")
        {
            Console.WriteLine("Interest addition cancelled.");
            return;
        }

        await Database.AccountManager.AddInterestAsync(account, amount, timeStamp).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Console.WriteLine($"Interest added.");
            }
            else
            {
                Console.WriteLine($"Error adding interest: {task.Exception?.Message}");
            }
        });
    }

    private static async Task PaymentCommand(string[] args)
    {
        if (args.Length is not 3 and not 4)
        {
            Console.WriteLine("Usage: payment <accountId> <amount> <payee> <opt | daysPrevious | 0");
            return;
        }

        if (!int.TryParse(args[0], out int accountId))
        {
            Console.WriteLine("Invalid account ID. Please enter a valid number.");
            return;
        }

        if (!double.TryParse(args[1], out double amount))
        {
            Console.WriteLine("Invalid amount. Please enter a valid number.");
            return;
        }

        string payee = args[2];

        Account? account = await Database.AccountManager.GetAccountAsync(accountId);
        if (account == null)
        {
            Console.WriteLine($"Account with ID {accountId} not found.");
            return;
        }

        double.Round(amount, 2);

        int dateOffset = 0;
        if (args.Length == 4 && !int.TryParse(args[3], out dateOffset))
        {
            Console.WriteLine("Invalid date offset. Please enter a valid number.");
            return;
        }

        DateTime timeStamp = DateTime.UtcNow.AddDays(-dateOffset);

        Console.WriteLine($"Make payment: {amount:C} to '{payee}' from account '{account.AccountName}'.\nConfirm? Y/[N]");
        string? confirm = Console.ReadLine()?.Trim().ToLower();

        if (confirm is not "y" and not "yes")
        {
            Console.WriteLine("Payment cancelled.");
            return;
        }

        await Database.AccountManager.MakePaymentAsync(account, amount, payee, timeStamp).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Console.WriteLine($"Payment made.");
            }
            else
            {
                Console.WriteLine($"Error making payment: {task.Exception?.Message}");
            }
        });
    }

    private static async Task PayeesCommand(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: payees <accountId>");
            return;
        }

        if (!int.TryParse(args[0], out int accountId))
        {
            Console.WriteLine("Invalid account ID. Please enter a valid number.");
            return;
        }

        Account? account = await Database.AccountManager.GetAccountAsync(accountId);
        if (account == null)
        {
            Console.WriteLine($"Account with ID {accountId} not found.");
            return;
        }

        Console.WriteLine($"Payees from {account.AccountName}:");
        List<string> payees = await AccountManager.GetPayeesAsync(account);
        foreach (string payee in payees)
        {
            Console.WriteLine($"\t{payee}");
        }
    }

    private static async Task TransferCommand(string[] args)
    {
        if (args.Length is not 3 and not 4)
        {
            Console.WriteLine("Usage: income <fromAccountId> <toAccountId> <value> <opt | daysPrevious | 0>");
            return;
        }

        if (!int.TryParse(args[0], out int fromAccountId))
        {
            Console.WriteLine("Invalid account ID. Please enter a valid number.");
            return;
        }

        if (!int.TryParse(args[1], out int toAccountId))
        {
            Console.WriteLine("Invalid account ID. Please enter a valid number.");
            return;
        }

        if (!double.TryParse(args[2], out double amount))
        {
            Console.WriteLine("Invalid amount. Please enter a valid number.");
            return;
        }

        Account? fromAccount = await Database.AccountManager.GetAccountAsync(fromAccountId);
        if (fromAccount == null)
        {
            Console.WriteLine($"Account with ID {toAccountId} not found.");
            return;
        }

        Account? toAccount = await Database.AccountManager.GetAccountAsync(toAccountId);
        if (toAccount == null)
        {
            Console.WriteLine($"Account with ID {toAccountId} not found.");
            return;
        }

        double.Round(amount, 2);

        int dateOffset = 0;
        if (args.Length == 4 && !int.TryParse(args[3], out dateOffset))
        {
            Console.WriteLine("Invalid date offset. Please enter a valid number.");
            return;
        }

        DateTime timeStamp = DateTime.UtcNow.AddDays(-dateOffset);

        Console.WriteLine($"Transfer: {amount:C} to account '{toAccount.AccountName}' from '{fromAccount.AccountName}'.\nConfirm? Y/[N]");
        string? confirm = Console.ReadLine()?.Trim().ToLower();

        if (confirm is not "y" and not "yes")
        {
            Console.WriteLine("Transfer cancelled.");
            return;
        }

        await AccountManager.MakeTransferAsync(toAccount, fromAccount, amount, timeStamp).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Console.WriteLine($"Transfer made.");
            }
            else
            {
                Console.WriteLine($"Error adding income: {task.Exception?.Message}");
            }
        });
    }

    private static async Task IncomeCommand(string[] args)
    {
        if (args.Length is not 3 and not 4)
        {
            Console.WriteLine("Usage: income <accountId> <amount> <source> <opt | daysPrevious | 0>");
            return;
        }

        if (!int.TryParse(args[0], out int accountId))
        {
            Console.WriteLine("Invalid account ID. Please enter a valid number.");
            return;
        }

        if (!double.TryParse(args[1], out double amount))
        {
            Console.WriteLine("Invalid amount. Please enter a valid number.");
            return;
        }

        string source = args[2];

        Account? account = await Database.AccountManager.GetAccountAsync(accountId);
        if (account == null)
        {
            Console.WriteLine($"Account with ID {accountId} not found.");
            return;
        }

        double.Round(amount, 2);

        int dateOffset = 0;
        if (args.Length == 4 && !int.TryParse(args[3], out dateOffset))
        {
            Console.WriteLine("Invalid date offset. Please enter a valid number.");
            return;
        }

        DateTime timeStamp = DateTime.UtcNow.AddDays(-dateOffset);

        Console.WriteLine($"Add income: {amount:C} to account '{account.AccountName}' from '{source}'.\nConfirm? Y/[N]");
        string? confirm = Console.ReadLine()?.Trim().ToLower();

        if (confirm is not "y" and not "yes")
        {
            Console.WriteLine("Income addition cancelled.");
            return;
        }

        await Database.AccountManager.AddIncomeAsync(account, amount, source, timeStamp).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Console.WriteLine($"Income added.");
            }
            else
            {
                Console.WriteLine($"Error adding income: {task.Exception?.Message}");
            }
        });
    }

    private static async Task AccountsCommand()
    {
        List<Account> accounts = await Database.AccountManager.GetAccountsAsync();
        foreach (Account account in accounts)
        {
            Console.WriteLine($"[{account.ID}] {account}");
        }
    }

    private static async Task EventsCommand(string[] args)
    {
        if (args.Length > 1)
        {
            Console.WriteLine("Usage: events <opt: accountId>");
            return;
        }

        List<AccountEvent> accountEvents;
        if (args.Length == 0)
        {
            accountEvents = await Database.AccountManager.GetAllAccountEventsAsync();
        }
        else
        {
            if (!int.TryParse(args[0], out int accountId))
            {
                Console.WriteLine("Invalid account ID. Please enter a valid number.");
                return;
            }

            accountEvents = await Database.AccountManager.GetAccountEventsByIdAsync(accountId);
        }

        foreach (AccountEvent accountEvent in accountEvents)
        {
            Console.WriteLine(accountEvent);
        }
    }
}
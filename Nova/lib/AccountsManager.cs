using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace Nova.lib;
internal class AccountsManager
{
    public static List<Account> Accounts =>
                                            (from account in Directory.GetFiles("N:\\Accounts", "*.json")
                                             let accountString = File.ReadAllText(account)
                                             select JsonSerializer.Deserialize<Account>(accountString) into account
                                             where account != null
                                             select account).ToList();

    public static bool WriteAccount(Account account)
    {
        if (!NovaDrive.DriveConnected)
            return false;

        if (!Directory.Exists("N:\\Accounts"))
            Directory.CreateDirectory("N:\\Accounts");

        if (File.Exists($"N:\\Accounts\\{account.AccountName.Replace(" ", string.Empty)}.json"))
            return false;
        
        string accountString = JsonSerializer.Serialize(account, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        File.WriteAllText($"N:\\Accounts\\{account.AccountName.Replace(" ", string.Empty)}.json", accountString);

        return true;
    }
}

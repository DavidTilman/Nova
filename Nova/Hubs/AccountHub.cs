using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

using Nova.Database;

namespace Nova.Hubs;
internal class AccountHub : Hub
{
    public async Task<List<Account>> GetAccountsAsync() => await AccountManager.GetAccountsAsync();
}

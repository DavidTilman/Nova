using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.Database;
internal class Config
{
    public static string ConnectionString = "Server = tcp:buget.database.windows.net,1433;Initial Catalog = buget; Encrypt=True;TrustServerCertificate=False;Connection Timeout = 30; Authentication=\"Active Directory Default\"";
}

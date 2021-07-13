using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace b2app
{
  using System.Text.RegularExpressions;

  public class Accounts : BackupCmd
  {
    public Accounts(Options.AccountsOpt o) { }

    public override int run(BUCommon.AccountList accts)
    {
      Console.WriteLine("Accounts:");

      foreach(var acc in accts.accounts)
        {
          Console.WriteLine("{0} - {1} ({2})", acc.name, acc.svcName, acc.connStr);
        }

      return 0;
    }
  }
}

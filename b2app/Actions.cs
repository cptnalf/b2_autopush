using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace b2app
{
  public class Actions
  {
    static BUCommon.AccountList accts;
    static BUCommon.Account acct;

    static void _Load(string account)
    {
      accts = BackupLib.AccountBuilder.BuildAccounts();
      
      if (!string.IsNullOrWhiteSpace(account))
        {
          acct = accts.Where(x => x.name == account).FirstOrDefault();
        }
    }
    static void _Save()
    {
      BackupLib.AccountBuilder.Save(accts);
    }
    
    internal static int Accounts(AccountsOpt o)
    {
      _Load(null);
      Console.WriteLine("Accounts:");

      foreach(var acc in accts)
        {
          Console.WriteLine("{0} - {1} ({2})", acc.name, acc.svcName, acc.connStr);
        }

      return 0;
    }

    internal static int ListContainers(ContOpt o)
    {
      _Load(o.account);

      var conts = new BackupLib.commands.Containers { account=acct, cache=accts.filecache};
      conts.run();
      _Save();
      var cs = accts.filecache.getContainers(acct.id);

      Console.WriteLine("Account: {0}", acct.name);
      foreach(var c in cs)
        { Console.WriteLine("{0} - {1}", c.name, c.type); }

      return 0;
    }

    internal static int ListFiles(LSOpt o)
    {
      _Load(o.account);

      var cont = accts.filecache.containers.Where(x => x.accountID==acct.id && x.name == o.container).ToList();

      var lsf = new BackupLib.commands.FileList { account=acct, cache=accts.filecache, versions=o.versions, useRemote=o.useremote, pathRE=o.filter };

      Console.WriteLine("Account: {0}", acct.name);
      foreach(var c in cont)
        {
          lsf.container=c;
          var files = lsf.run();

          Console.WriteLine();
          Console.WriteLine("Container: {0}", c.name);
          foreach(var f in files.OrderBy(x => x.path))
            { Console.WriteLine("{0}, {1}", f.uploaded, f.path); }
        }
      
      _Save();
      return 0;
    }
  }
}

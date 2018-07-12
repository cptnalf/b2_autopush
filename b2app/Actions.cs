using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace b2app
{
  using System.Text.RegularExpressions;

  public abstract class BackupCmd
  {
    public abstract int run(BUCommon.AccountList accts);

    protected BUCommon.Account _getAcct(BUCommon.AccountList accts, string account)
    {
      BUCommon.Account acct = null;
      if (!string.IsNullOrWhiteSpace(account))
        {
          acct = accts.Where(x => x.name == account).FirstOrDefault();
        }
      return acct;
    }
  }

  public class Accounts : BackupCmd
  {
    public Accounts(Options.AccountsOpt o) { }

    public override int run(BUCommon.AccountList accts)
    {
      Console.WriteLine("Accounts:");

      foreach(var acc in accts)
        {
          Console.WriteLine("{0} - {1} ({2})", acc.name, acc.svcName, acc.connStr);
        }

      return 0;
    }
  }

  public class Auth : BackupCmd
  {
    private string _acctname;
    public Auth(Options.AuthOpt o)
    {
      _acctname = o.account;
    }

    public override int run(BUCommon.AccountList accounts)
    {
      var acct = _getAcct(accounts, _acctname);

      Console.WriteLine("account: {0}", acct.name);
      (new BackupLib.commands.Authorize 
        { 
          account=acct
          , accounts=accounts
        }).run();

      return 0;
    }
  }

  public class ListContainers : BackupCmd
  {
    private string _acct;
    public ListContainers(Options.ContOpt o) { _acct = o.account; }

    public override int run(BUCommon.AccountList accounts)
    {
      var acct = _getAcct(accounts, _acct);
      var conts = new BackupLib.commands.Containers { account=acct, cache=accounts.filecache};
      conts.run();

      var cs = accounts.filecache.getContainers(acct.id);
      Console.WriteLine("Account: {0}", acct.name);
      foreach(var c in cs)
        { Console.WriteLine("{0} - {1}", c.name, c.type); }

      return 0;
    }
  }

  internal class CopyLoc
  {
    public BUCommon.Container cont {get;set;}
    public BUCommon.Account acct {get;set;}
  }

  internal abstract class ProgBackupCmd : BackupCmd
  {
    protected void _printDiff(BackupLib.FileDiff x)
    { Console.WriteLine("{0} - {1}", x.type, (x.local != null ? x.local.path : x.remote.path)); }

    protected void _printExcept(BackupLib.FileDiff x, Exception e)
    { }

    protected int _err(string format, params object[] args)
    {
      Console.WriteLine(format, args);
      return 1;
    }

    protected CopyLoc _parseLoc(BUCommon.AccountList accts, string loc)
    {
      CopyLoc l = new CopyLoc();

      if (System.IO.Directory.Exists(loc)) 
        { l.cont = new BUCommon.Container { accountID=0, id=loc, name=loc, type="LOCAL"}; }
      else
        {
          var parts = loc.Split(':');
          if (parts != null && parts.Length > 1)
            {
              if (parts[0].Length == 1) 
                { 
                  return l;
                }

              var acct = accts.accounts.Where(x => x.name == parts[0]).FirstOrDefault();
              if (acct != null)
                {
                  var cont = accts.filecache.containers.Where(x => x.accountID == acct.id && x.name== parts[1]).FirstOrDefault();

                  if (cont != null)
                    { l.acct=acct; l.cont=cont; }
                }
            }
        }

      return l;
    }
  }


}

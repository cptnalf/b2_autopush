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

  public class ListFiles : BackupCmd
  {
    private Options.LSOpt _opts;        
    public ListFiles(Options.LSOpt o)  { _opts = o; }

    public override int run(BUCommon.AccountList accounts)
    {
      var acct = _getAcct(accounts, _opts.account);
      var cmd = new BackupLib.commands.FileList 
        { 
          account=acct
          , cache=accounts.filecache
          , versions=_opts.versions
          , useRemote=_opts.useremote
          , pathRE=_opts.filter 
        };
    
      var containers = accounts.filecache.containers
          .Where(x => x.accountID==acct.id && x.name == _opts.container)
          .ToList();

      Console.WriteLine("Account: {0}", acct.name);
      foreach(var c in containers)
        {
          cmd.container=c;
          var files = cmd.run();

          Console.WriteLine();
          Console.WriteLine("Container: {0}", c.name);
          foreach(var f in files.OrderBy(x => x.path))
            { Console.WriteLine("{0}, {1}", f.uploaded, f.path); }
        }

      return 0;
    }
  }

  public abstract class ProgBackupCmd : BackupCmd
  {
    protected void _printDiff(BackupLib.FileDiff x)
    { Console.WriteLine("{0} - {1}", x.type, (x.local != null ? x.local.path : x.remote.path)); }

    protected void _printExcept(BackupLib.FileDiff x, Exception e)
    { }
  }

  public class Sync : ProgBackupCmd
  {
    private Options.SyncOpts _opts;

    public Sync(Options.SyncOpts o) { _opts = o; }

    public override int run(BUCommon.AccountList accounts)
    {
      var account = _getAcct(accounts,_opts.account); 
      var cont = accounts.filecache.containers
        .Where(x => x.accountID==account.id && x.name == _opts.container)
        .ToList();

      var cmd = new BackupLib.commands.Sync 
        { 
            account=account
          , cache=accounts.filecache
          , container=cont.FirstOrDefault()
          , progress=_printDiff
          , excepts = _printExcept

          ,noAction=_opts.dryrun
          , keyFile=_opts.keyfile
          , pathRoot=_opts.pathroot
          , useRemote=_opts.useremote
          , privateKey=_opts.privateKey
          , filterRE=_opts.filterre
          , excludeRE=_opts.excludere
          ,maxTasks = _opts.maxTasks
          ,checksum = _opts.cksum
        };

      Console.WriteLine("Account: {0}", account.name);
      Console.WriteLine("Container: {0}", cmd.container.name);

      cmd.run();
      
      return 0;
    }
  }

  public class Copy : ProgBackupCmd
  {
    internal class CopyLoc
    {
      public BUCommon.Container cont {get;set;}
      public BUCommon.Account acct {get;set;}
    }

    private Options.CopyOpts _opts;
    public Copy(Options.CopyOpts o) { _opts = o; }

    public override int run(BUCommon.AccountList accounts)
    {
      var src = _ParseLoc(accounts, _opts.source);
      var dest = _ParseLoc(accounts, _opts.dest);

      if (src.cont == null) { return _err("ERROR - source is unknown: {0}", _opts.source); }
      if (dest.cont == null) { return _err("ERROR - dest is unknown: {0}", _opts.dest); }

      if (src.acct == null && dest.acct == null)
        { return _err("ERROR - use copy instead of this program (both are local)."); }
      
      if (src.acct != null && dest.acct != null)
        { return _err("ERROR - remote to remote copying is not yet supported."); }

      /* these options should be mutually exclusive. */
      if (dest.acct != null)
        {
          var crm = new BackupLib.commands.CopyRemote
              {
                  cache=accounts.filecache
                  , account=dest.acct
                  , container=dest.cont
                  , pathRoot=src.cont.id

                  , progress= _printDiff

                  , fileRE=_opts.filterRE
                  , key=_opts.key
                  , noAction=_opts.dryrun
              };
          crm.run();
        }

      if (src.acct != null && !string.IsNullOrWhiteSpace(_opts.filterRE))
        {
          var cl = new BackupLib.commands.CopyLocal 
            { 
              account=src.acct
              , destPath=dest.cont.id
              , filterre = _opts.filterRE
              , key=_opts.key
              , noAction=_opts.dryrun
              , progress= _printDiff
              , errors = _printExcept
            };
          cl.run();
        }
      
      return 0;
    }

    private CopyLoc _ParseLoc(BUCommon.AccountList accts, string loc)
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

    private int _err(string format, params object[] args)
    {
      Console.WriteLine(format, args);
      return 1;
    }
  }
}

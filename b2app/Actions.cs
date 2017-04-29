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
    
    internal static int Accounts(Options.AccountsOpt o)
    {
      _Load(null);
      Console.WriteLine("Accounts:");

      foreach(var acc in accts)
        {
          Console.WriteLine("{0} - {1} ({2})", acc.name, acc.svcName, acc.connStr);
        }

      return 0;
    }

    internal static int ListContainers(Options.ContOpt o)
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

    internal static int ListFiles(Options.LSOpt o)
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

    internal static int Copy(Options.CopyOpts o)
    {
      _Load(null);

      var src = Options.CopyOpts.ParseLoc(accts, o.source);
      var dest = Options.CopyOpts.ParseLoc(accts, o.dest);

      if (src.cont == null) { Console.WriteLine("ERROR - source is unknown: {0}", o.source); return 1; }
      if (dest.cont == null) { Console.WriteLine("ERROR - dest is unknown: {0}", o.dest); return 1; }

      if (src.acct == null && dest.acct == null)
        { Console.WriteLine("ERROR - use copy instead of this program (both are local)."); return 1; }
      
      if (src.acct != null && dest.acct != null)
        { Console.WriteLine("ERROR - remote to remote copying is not yet supported."); return 1; }

      if (dest.acct != null)
        {
          var crm = new BackupLib.commands.CopyRemote
              {
                 account=dest.acct
                 , cache=accts.filecache
                 , container=dest.cont
                 , fileRE=o.filterRE
                 , key=o.key
                 , pathRoot=src.cont.id
                 , noAction=o.dryrun
                 , progress= _PrintDiff
              };
          crm.run();
        }
      if (src.acct != null && !string.IsNullOrWhiteSpace(o.filterRE))
        {
          var filterre = new System.Text.RegularExpressions.Regex(o.filterRE
          , System.Text.RegularExpressions.RegexOptions.Compiled| System.Text.RegularExpressions.RegexOptions.IgnoreCase);
          var files = accts.filecache.getContainer(src.acct.id, src.cont.id, null).Where(x => filterre.IsMatch(x.path)).ToList();

          if (files.Count > 10) { Console.WriteLine("Limited!"); return 1; }
          var cl = new BackupLib.commands.CopyLocal 
            { 
              account=src.acct
              , key=o.key
              , destPath=dest.cont.id
              , file=null
              , noAction=o.dryrun
              , progress= _PrintDiff
            };
          foreach(var f in files)
            {
              cl.file=f;
              cl.run();
            }
        }
      
      return 0;
    }

    private static void _PrintDiff(BackupLib.FileDiff x)
    { Console.WriteLine("{0} - {1}", x.type, (x.local != null ? x.local.path : x.remote.path)); }
  }
}

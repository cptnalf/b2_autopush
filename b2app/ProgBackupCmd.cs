using System;
using System.Linq;

namespace b2app
{
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
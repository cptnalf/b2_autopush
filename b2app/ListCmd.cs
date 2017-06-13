using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace b2app
{
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
}

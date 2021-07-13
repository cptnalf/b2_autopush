using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace b2app
{
  internal class Sync : ProgBackupCmd
  {
    private Options.SyncOpts _opts;

    public Sync(Options.SyncOpts o) { _opts = o; }

    public override int run(BUCommon.AccountList accounts)
    {
      var account = _getAcct(accounts,_opts.account); 
      var cont = accounts.filecache.containers
        .Where(x => x.accountID==account.id && x.name == _opts.container)
        .FirstOrDefault();

      var cmd = new BackupLib.commands.Sync 
        { 
            accountList = accounts
          , account=account
          , cache=accounts.filecache
          , container=cont
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
      Console.WriteLine("Encrypting using {0}", cont.encType == "AGE" ? "age" : "built-in");

      cmd.run();
      
      return 0;
    }
  }
}

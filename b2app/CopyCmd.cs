using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace b2app
{
  internal class Copy : ProgBackupCmd
  {
    private Options.CopyOpts _opts;
    public Copy(Options.CopyOpts o) { _opts = o; }

    public override int run(BUCommon.AccountList accounts)
    {
      var src = _parseLoc(accounts, _opts.source);
      var dest = _parseLoc(accounts, _opts.dest);

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
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using BUCommon;

namespace b2app
{
  internal class EditContainer : ProgBackupCmd
  {
    private Options.EditContainer _config;
    public EditContainer(Options.EditContainer opts) { _config = opts; }

    public override int run(AccountList accts)
    {
      var acct = _getAcct(accts, _config.account);

      if (acct == null) 
        {
          Console.Error.WriteLine("Account not found.");
          return 1;
        }

      var cont = accts.filecache.containers
        .Where(x => x.accountID == acct.id && x.name == _config.container)
        .FirstOrDefault();

      if (cont == null)
        {
          Console.Error.WriteLine("Container {0} not found in account {1}", _config.container,acct.name);
          Console.Error.WriteLine("maybe refresh containers from remote first?");
          return 1;
        }


      string enc = string.Empty;
      if (_config.useAge) { enc = "AGE"; }

      Console.WriteLine("Changing encryption for container {0} to <{1}>", _config.container
        , enc == "AGE" ? "age" : "built-in");
      
      cont.encType = enc;

      return 0;
    }
  }
}
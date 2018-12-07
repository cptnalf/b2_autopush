using System;

namespace b2app
{
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
}
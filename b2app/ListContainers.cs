using System;

namespace b2app
{
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
}

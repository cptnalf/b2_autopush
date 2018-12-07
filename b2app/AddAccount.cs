using System;
using BUCommon;

namespace b2app
{
  public class AddAccount : BackupCmd
  {
    private readonly Options.AddAccountOptions _opts;
    public AddAccount(Options.AddAccountOptions opts)
    {
      _opts = opts;
    }
    
    public override int run(AccountList accts)
    {
      /* validate the options. */
      Account acct = new Account();

      acct.name = _opts.name;
      if (string.IsNullOrWhiteSpace(acct.name))
        {
          Console.WriteLine("Account name can't be blank.");
          return -1;
        }
      
      if (string.Compare(_opts.type, "local", true) == 0)
        { 
          acct.svcName = typeof(BackupLib.LocalService).FullName;
          acct.connStr = _opts.directory;
        }
      if (string.Compare(_opts.type, "b2", true) == 0)
        {
          acct.svcName = typeof(CommB2.Connection).FullName;
          acct.connStr = string.Format("{0}:{1}", _opts.user, _opts.password);
        }
      
      if (string.IsNullOrWhiteSpace(acct.svcName))
        {
          Console.WriteLine("Account type unknown: valid types: local, b2");
          return -1;
        }
      
      accts.Add(acct);
      BackupLib.AccountBuilder.Load(accts, acct);
      return 0;
    }
  }
}
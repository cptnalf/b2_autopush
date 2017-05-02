using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib.commands
{
  public class Authorize : BUCommon.ICommand
  {
    public BUCommon.AccountList accounts {get;set;}
    public BUCommon.Account account {get;set;}

    public void run()
    {
      account.service.authorize();

      AccountBuilder.Save(accounts);
    }
  }
}

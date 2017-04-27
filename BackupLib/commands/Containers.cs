using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib.commands
{
  public class Containers : BUCommon.ICommand
  {
    public string helptext => @"lsc <account>
        - list containers on the account's service
";
    
    public BUCommon.FileCache cache {get;set;}
    public BUCommon.Account account {get;set;}

    public void run()
    {
      var conts = account.service.getContainers();
      
      foreach(var c in conts) { cache.add(c); }

      var lst = cache.containers.Where(x => x.accountID == account.id);
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib.commands
{
  public class Authorize
  {
    public BUCommon.Account account {get;set;}

    public void run()
    {
      account.service.authorize();
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib.commands
{
  class Containers
  {
    public BUCommon.FileCache cache {get;set;}
    public BUCommon.Account account {get;set;}

    public void run()
    {
      var conts = account.service.getContainers();

      List<BUCommon.Container> todel = new List<BUCommon.Container>();
      foreach(var c in cache.containers.Where(x => x.accountID == account.id))
        { if (!conts.Where(x => x.id == c.id).Any()) { todel.Add(c) ;} }

      foreach(var d in todel) { cache.delete(d); }
      foreach(var c in conts) { cache.add(c); }

      var lst = cache.containers.Where(x => x.accountID == account.id);
    }
  }
}

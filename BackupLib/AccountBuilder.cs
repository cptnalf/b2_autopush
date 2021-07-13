using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib
{
  using System.IO;

  public class AccountBuilder
  {
    private static Dictionary<string,Func<BUCommon.IFileSvc>> _SvcMapping = new Dictionary<string,Func<BUCommon.IFileSvc>>();

    static AccountBuilder()
    {
      _SvcMapping.Add(typeof(CommB2.Connection).FullName, () => new CommB2.Connection());
      _SvcMapping.Add(typeof(LocalService).FullName, () => new LocalService());
    }

    public static BUCommon.AccountList BuildAccounts()
    {
      string file = "b2app.accounts.xml";
      file = System.IO.Path.Combine(BUCommon.ConfigPaths.Prefix, file);

      var acctlst = new BUCommon.AccountList();
      acctlst.load(file);

      file = "b2app.filecache";
      file = System.IO.Path.Combine(BUCommon.ConfigPaths.Prefix, file);
      BUCommon.FileCache fc = null;
      if (System.IO.File.Exists(file + ".json"))
        { fc = BUCommon.FileCache.Load(file + ".json"); }
      else
        {
          if (System.IO.File.Exists(file + ".xml"))
            {
              fc = BUCommon.FileCache.Load(file + ".xml");
              fc.save(file + ".json");
            }
          else
            { fc = BUCommon.FileCache.Load(file + ".json"); }
        }
      
      acctlst.filecache = fc;

      foreach(var a in acctlst.accounts) { Load(acctlst, a); }

      return acctlst;
    }

    public static void Save(BUCommon.AccountList accounts)
    {
      string file = "b2app.accounts.xml";
      file = Path.Combine(BUCommon.ConfigPaths.Prefix, file);
      accounts.save(file);

      file = "b2app.filecache.json";
      file = Path.Combine(BUCommon.ConfigPaths.Prefix, file);
      accounts.filecache.save(file);
    }

    public static void Load(BUCommon.AccountList accounts, BUCommon.Account account)
    {
      Func<BUCommon.IFileSvc> svc = null;
      if (_SvcMapping.TryGetValue(account.svcName, out svc))
        {
          account.service = svc();
          account.service.account = account;
          account.service.fileCache = accounts.filecache;
          account.service.setParams(account.connStr);
        }
      else
        { account.service = null; }
    }
  }
}

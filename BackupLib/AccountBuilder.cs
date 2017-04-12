using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib
{
  public class AccountBuilder
  {
    private static Dictionary<string,Func<BUCommon.IFileSvc>> _SvcMapping = new Dictionary<string,Func<BUCommon.IFileSvc>>();

    static AccountBuilder()
    {
      _SvcMapping.Add(typeof(CommB2.Connection).FullName, () => new CommB2.Connection());
    }

    public static BUCommon.AccountList BuildAccounts()
    {
      string file = "b2app.accounts.xml";
      file = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), file);

      var acctlst = new BUCommon.AccountList();
      acctlst.load(file);

      foreach(var a in acctlst) { Load(a); }

      return acctlst;
    }

    public static void Save(BUCommon.AccountList accounts)
    {
      string file = "b2app.accounts.xml";
      file = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), file);
      accounts.save(file);
    }

    public static void Load(BUCommon.Account account)
    {
      Func<BUCommon.IFileSvc> svc = null;
      if (_SvcMapping.TryGetValue(account.svcName, out svc))
        {
          account.service = svc();
          account.service.setParams(account.connStr);
        }
      else
        { account.service = null; }
    }
  }
}

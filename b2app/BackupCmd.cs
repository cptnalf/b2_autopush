using System.Linq;

namespace b2app
{
   public abstract class BackupCmd
  {
    public abstract int run(BUCommon.AccountList accts);

    protected BUCommon.Account _getAcct(BUCommon.AccountList accts, string account)
    {
      BUCommon.Account acct = null;
      if (!string.IsNullOrWhiteSpace(account))
        {
          acct = accts.Where(x => x.name == account).FirstOrDefault();
        }
      return acct;
    }
  } 
}
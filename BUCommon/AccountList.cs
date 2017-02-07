using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUCommon
{
  using System.Xml.Serialization;

  public class AccountList : IEnumerable<Account>
  {
    [XmlIgnore]
    private long _maxID = 0;

    public List<Account> accounts {get;set; }
    public AccountList() { this.accounts = new List<Account>(); }

    /// <summary>
    /// create a new account
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Account create(string name)
    {
      _maxID++;
      var newa = new Account { id=_maxID, name=name };
      this.accounts.Add(newa);

      return newa;
    }

    /// <summary>
    /// load an account from a file.
    /// </summary>
    /// <param name="file"></param>
    public void load(string file)
    {
      var accts = XmlUtils.ReadXml<AccountList>(file, new Type[] { typeof(Account)});
      this.accounts = (accts.accounts != null ? accts.accounts : this.accounts);

      _maxID = this.accounts.Max((x)=> x.id);
    }

    /// <summary>
    /// save an account to a file.
    /// </summary>
    /// <param name="file"></param>
    public void save(string file)
    { XmlUtils.WriteXml(file, this, new Type[] { typeof(Account)}); }

    public IEnumerator<Account> GetEnumerator() { return this.accounts.GetEnumerator(); }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this.accounts.GetEnumerator(); }
  }
}
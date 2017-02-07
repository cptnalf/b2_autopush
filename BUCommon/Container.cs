using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUCommon
{
  using System.Xml.Serialization;

  public class AccountList
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
      return new Account { id=_maxID, name=name };
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
  }

  /// <summary>
  /// an account.
  /// </summary>
  public class Account
  {
    public long id;
    public string svcName;
    public string connStr;
    public string name;

    [XmlIgnore]
    public IFileSvc service {get;set; }
  }

  /// <summary>
  /// a container for files. (usually called a bucket)
  /// </summary>
  public class Container
  {
    /// <summary>
    ///  account this container belongs to
    /// </summary>
    public long accountID {get;set; }
    /// <summary>
    /// it's service-defined identifier.
    /// </summary>
    public string id {get;set; }
    /// <summary>a human-readable name for the container.</summary>
    public string name {get;set; }

    [XmlIgnore]
    public Account account {get;set; }
  }
}

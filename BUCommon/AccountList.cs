using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUCommon
{
  using System.Xml.Serialization;

  public class AccountList //: IEnumerable<Account>
  {
    [XmlIgnore]
    private long _maxID = 0;

    [XmlIgnore]
    public FileCache filecache {get;set;}
    public List<Account> accounts {get;set; }
    public string AgePath {get;set; } 

    public AccountList() { this.accounts = new List<Account>(); }

    public void Add(object o)
    {
      Account a = o as Account;
      if (a == null && o != null)
        { throw new ArgumentException("object must be an Account."); }

      accounts.Add(a);
      if (_maxID < a.id) { _maxID = a.id; }
    }

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
      AccountList accts = null;
      bool tryagain = false;
      try {
        accts = XmlUtils.ReadXml<AccountList>(file, new Type[] { typeof(Account), typeof(AuthStorage), typeof(AuthStorage.Pair)});
        this.accounts = (accts == null ? this.accounts : (accts.accounts != null ? accts.accounts : this.accounts));
        this.AgePath = accts.AgePath;
      }
      catch(System.InvalidOperationException ioe)
        { tryagain = true; }
      
      if (tryagain)
        {
          /* try to load the original list. */
          var x = XmlUtils.ReadXml<Account[]>(file, new Type[]{typeof(Account), typeof(AuthStorage), typeof(AuthStorage.Pair)});

          this.accounts.Clear();
          this.accounts.AddRange(x);
        }

      _maxID = this.accounts.Any() ? this.accounts.Max((x)=> x.id) : 0;
    }

    /// <summary>
    /// save an account to a file.
    /// </summary>
    /// <param name="file"></param>
    public void save(string file)
    {
      XmlUtils.WriteXml(file, this, new Type[] { typeof(Account), typeof(AuthStorage), typeof(AuthStorage.Pair)}); 
    }

    //public IEnumerator<Account> GetEnumerator() { return this.accounts.GetEnumerator(); }
    //System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return this.accounts.GetEnumerator(); }
  }
}
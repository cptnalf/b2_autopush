namespace BUCommon
{
 using System.Xml.Serialization;

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

    public AuthStorage auth {get;set;}

    public Account() { this.auth = new AuthStorage(); }
  }
}

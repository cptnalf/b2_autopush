using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

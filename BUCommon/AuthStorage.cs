using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUCommon
{
  /// <summary>
  /// store authorization info.
  /// </summary>
  public class AuthStorage
  {
    public List<KeyValuePair<string,string>> parameters {get;set;}

    public string this[string key]
    {
      get 
      {
        var at = parameters.Where(x => x.Key == key).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(at.Key)) { return at.Value; }
        return null;
      }
    }

    public AuthStorage() { this.parameters = new List<KeyValuePair<string, string>>(); }

    public void add(string name, string value) { this.parameters.Add(new KeyValuePair<string, string>(name,value)); }

    public void save(string file)
    {
      XmlUtils.WriteXml(file, this, new Type[] { typeof(KeyValuePair<string,string>)});
    }

    public void load(string file)
    {
      var o = XmlUtils.ReadXml<AuthStorage>(file, new Type[] { typeof(KeyValuePair<string,string>)});
      this.parameters = o.parameters;
    }
  }
}

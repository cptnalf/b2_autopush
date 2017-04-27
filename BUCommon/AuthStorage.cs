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
    public class Pair 
    { 
      public string key; 
      public string value; 

      public Pair(string k, string v) { key=k; value=v; } 
      public Pair() { }
    }

    public List<Pair> parameters {get;set;}

    public string this[string key]
    {
      get 
      {
        var at = parameters.Where(x => x.key == key).FirstOrDefault();
        if (at != null) { return at.value; }
        return null;
      }
      set
      {
        var at = parameters.Where(x => x.key == key).FirstOrDefault();
        if (at == null) { parameters.Add(new Pair(key, value)); }
        else
          { at.value = value; }
      }
    }

    public AuthStorage() { this.parameters = new List<Pair>(); }

    public void add(string name, string value) { this.parameters.Add(new Pair(name,value)); }

    public void save(string file)
    {
      XmlUtils.WriteXml(file, this, new Type[] { typeof(Pair)});
    }

    public void load(string file)
    {
      var o = XmlUtils.ReadXml<AuthStorage>(file, new Type[] { typeof(Pair)});
      this.parameters = o.parameters;
    }
  }
}

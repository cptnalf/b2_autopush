using System;
using System.Linq;

namespace BUCommon
{
  using System.Xml.Serialization;

  public class Hash
  {
    public static Hash Create(string type, byte[] hash)
    { 
      var res = new Hash { type=type, raw=hash }; 
      res.base64 = Convert.ToBase64String(res.raw);
      return res;
    }
    
    public static Hash FromString(string type, string hash)
    {
      byte[] bytes = Enumerable
        .Range(0, hash.Length)
        .Where(x => x % 2 == 0)
        .Select(x => Convert.ToByte(hash.Substring(x, 2), 16))
        .ToArray();
      
      return new Hash { type=type, base64=Convert.ToBase64String(bytes), raw=bytes};
    }

    public static Hash Create(string type, string hash64)
    {
      var res = new Hash { type=type, base64=hash64, raw=Convert.FromBase64String(hash64) };
      return res;
    }

    public int id {get;set}

    /// <summary>hash type</summary>
    /// <example>
    /// SHA
    /// MD5
    /// SHA-1
    /// SHA-256
    /// </example>
    public string type { get;set; }

    /// <summary>raw bytes of the hash</summary>
    [XmlIgnore]
    public byte[] raw { get;set; }

    /// <summary>computed base-64 encoded string</summary>
    public string base64 { get;set; }
  }
}

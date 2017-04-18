using System;

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
    
    public static Hash Create(string type, string hash)
    {
      var res = new Hash { type=type, base64=hash, raw=Convert.FromBase64String(hash) };
      return res;
    }

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

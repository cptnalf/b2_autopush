using System;
using System.Xml.Serialization;

namespace BUCommon
{
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

  public class FreezeFile
  {
    /// <summary>non-rooted path to the file.</summary>
    public string path {get;set; }

    /// <summary>mime type of the file for b2</summary>
    public string mimeType {get;set; }

    /// <summary>hash for the encrypted contents as it's stored in the provider.</summary>
    public Hash storedHash {get;set; }

    /// <summary>hash for the non-encrypted contents</summary>
    public Hash localHash {get;set; }

    public DateTime modified {get;set; }
    /// <summary>when this file was uploaded to the provider</summary>
    public DateTime uploaded {get;set; }

    public string fileID {get;set; }
    
    public FreezeFile() { }
  }
}

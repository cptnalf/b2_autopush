using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackupLib
{
  public class Hash
  {
    /// <summary>hash type</summary>
    /// <example>
    /// SHA
    /// MD5
    /// SHA-1
    /// SHA-256
    /// </example>
    public string type {get;set; }

    /// <summary>raw bytes of the hash</summary>
    public byte[] raw {get;set; }

    /// <summary>computed base-64 encoded string</summary>
    public string B64 
    { 
      get { return Convert.ToBase64String(this.raw); }
      set 
      {
        this.raw = Convert.FromBase64String(value);
      }
    }
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

    /// <summary>when this file was uploaded to the provider</summary>
    public DateTime uploaded {get;set; }
    
    public FreezeFile() { }
  }
}

using System;
using System.Xml.Serialization;

namespace BUCommon
{
  public class FreezeFile
  {
    private Container _cont;

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

    /// <summary>cloud provider ID</summary>
    public string fileID {get;set; }

    public string serviceInfo {get;set;}
    public string enchash {get;set;}
   
    public string containerID {get;set;}

    [XmlIgnore]
    public long id {get;set;}

    /// <summary>this is the hash of the local contents as they were sent to the provider.</summary>
    [XmlIgnore]
    public Hash lastHash {get;set;}

    /// <summary>the container this file belongs to (null if not on a provider)</summary>
    [XmlIgnore]
    public Container container
    {
      get { return _cont; }
      set 
      {
        _cont = value;
        if (this.containerID != _cont.id) { this.containerID = _cont.id; }
      }
    }
    
    public FreezeFile() { }

    public System.IO.Stream readStream(string baseDir)
    {
      return new System.IO.FileStream(System.IO.Path.Combine(baseDir, path), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete);
    }
  }
}

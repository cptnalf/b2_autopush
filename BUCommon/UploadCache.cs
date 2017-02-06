using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BUCommon
{
  using System.Globalization;
  using System.IO;

  public class UploadCache
  {
    [XmlRoot]
    public class UploadCacheXml
    {
      public List<FreezeFile> files {get;set; }
      public UploadCacheXml() { this.files = new List<FreezeFile>(); }
    }
    
    private List<FreezeFile> _files = new List<FreezeFile>();
    
    public void add(FreezeFile ff) { _files.Add(ff); }

    public IReadOnlyList<FreezeFile> getdir(string folder)
    {
      var lst = 
        from f in _files 
        where CultureInfo.InvariantCulture.CompareInfo.IsPrefix(f.path, folder, CompareOptions.IgnoreCase)
        select f;

      return lst.ToArray();
    }

    public void write(string file)
    {
      var strm = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read);

      var xdr = _serializerMake();
      var ucx = new UploadCacheXml();
      ucx.files.AddRange(_files);
      xdr.Serialize(strm, ucx);
      strm.Close();
      strm.Dispose();
      strm = null;
      xdr = null;
    }

    public void read(string file)
    {
      var strm = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      var xdr  = _serializerMake();
      var ucx = xdr.Deserialize(strm) as UploadCacheXml;
      xdr = null;
      strm.Close();
      strm.Dispose();
      strm = null;
      
      _files.Clear();
      if (ucx != null)
        { _files.AddRange(ucx.files); }
    }

    private XmlSerializer _serializerMake() 
    { return new XmlSerializer(typeof(UploadCacheXml), new Type[] { typeof(FreezeFile), typeof(Hash)}); }
  }
}

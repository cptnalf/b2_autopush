using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace BUCommon
{
  public class Version1_0Storage
  {
    [XmlRoot]
    public class FileCacheXml
    {
      public List<Container> containers {get;set; }
      public FileCacheXml() { this.containers = new List<Container>(); }
    }

    public static Version1_0Storage Build(string filename)
    {
      if (string.Compare(".xml", Path.GetExtension(filename), true) != 0)
        { throw new ArgumentException("only xml files!"); }
      
      if (!File.Exists(filename))
        { throw new ArgumentException("file dne!"); }
      
      var v1s = new Version1_0Storage();

      v1s.load(filename);

      return v1s;
    }
    
    private List<Container> _containers;
    private List<FreezeFile> _files;
    public IReadOnlyList<Container> containers { get { return _containers; } }
    public IReadOnlyList<FreezeFile> files { get { return _files; } }


    protected Version1_0Storage()
    {
      _containers = new List<Container>();
      _files = new List<FreezeFile>();
    }

    protected void load(string filename)
    {
      var strm = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      var xdr  = _serializerMake();
      var ucx = xdr.Deserialize(strm) as FileCacheXml;
      xdr = null;
      strm.Close();
      strm.Dispose();
      strm = null;
      
      _containers.Clear();
      _files.Clear();
      if (ucx != null)
        { foreach(var c in ucx.containers) { add(c); } }
    }

    protected Container add(Container cont)
    {
      foreach(var f in cont.files) 
        { 
          if (f.container == null) { f.container = cont; }
          add(f);
        }

      var oldc = _containers.Where(x => x.id == cont.id && x.accountID == cont.accountID).FirstOrDefault();

      if (oldc != null)
        {
          /* well shit. */
          cont = oldc;
        }
      else 
        { _containers.Add(cont); oldc=cont; }
      
      cont.files.Clear();
      foreach(var f in _files
                  .Where(x => x.container != null && x.container.id == oldc.id && x.container.accountID == oldc.accountID))
        {
          cont.files.Add(f); 
          f.container = cont;
        }

      return cont;
    }

    public void add(FreezeFile ff)
    {
      FreezeFile oldf = null;
      
      if (ff.container == null)
        {
          oldf = _files
            .Where(x =>    (x.container == null)
                        && (   (!string.IsNullOrWhiteSpace(x.fileID) && x.fileID == ff.fileID) 
                            || (string.IsNullOrWhiteSpace(x.fileID) && x.path == ff.path)
                            )
                   )
            .FirstOrDefault();
        }
      else
        {
          oldf = _files
            .Where(x =>    (   x.container != null 
                               && (x.container == ff.container
                                   || (x.container.accountID == ff.container.accountID && x.container.id == ff.container.id))
                           )
                        && (   (!string.IsNullOrWhiteSpace(x.fileID) && x.fileID == ff.fileID) 
                            || (string.IsNullOrWhiteSpace(x.fileID) && x.path == ff.path)
                            )
                   )
            .FirstOrDefault();          
        }

      if (oldf == null) { _files.Add(ff); }
      else
        {
          if (oldf.localHash != null && ff.localHash == null)
            { ff.localHash = oldf.localHash; }
          if (oldf.storedHash != null && ff.storedHash == null)
            { ff.storedHash = oldf.storedHash; }
          if (oldf.uploaded != DateTime.MinValue && ff.uploaded == DateTime.MinValue)
            { ff.uploaded = oldf.uploaded; }

          _files.Remove(oldf);
          _files.Add(ff);
        }

      if (ff.container != null)
        {
          var c = _containers.Where(x => x.accountID == ff.container.accountID && x.id == ff.container.id).FirstOrDefault();
          if (c !=null) 
            {
              var f1 = c.files.Where(x => x.fileID == ff.fileID).FirstOrDefault();
              if (f1 != null) { c.files.Remove(f1); }
              c.files.Add(ff);
            }
        }
    }

    private XmlSerializer _serializerMake() 
    { return new XmlSerializer(typeof(FileCacheXml), new Type[] { typeof(FreezeFile), typeof(Hash)}); }
  }
}
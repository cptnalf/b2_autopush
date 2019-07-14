using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BUCommon
{
  using System.Globalization;
  using System.IO;

  public class FileCache
  {
    [XmlRoot]
    public class FileCacheXml
    {
      public List<Container> containers {get;set; }
      public FileCacheXml() { this.containers = new List<Container>(); }
    }
    
    private List<FreezeFile> _files = new List<FreezeFile>();
    private List<Container> _containers = new List<Container>();

    public IReadOnlyList<FreezeFile> files { get { return _files;} }
    public IReadOnlyList<Container> containers { get { return _containers; } }

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

    public Container add(Container cont)
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

    /// <summary>
    /// purge the container and all files in it from the cache.
    /// </summary>
    /// <param name="cont"></param>
    public void delete(Container cont)
    {
      var c1 = _containers.Where(x => x.accountID == cont.accountID && x.id == cont.id).FirstOrDefault();
      if (c1 != null) 
        { 
          _containers.Remove(c1);
          BUCommon.FreezeFile ff = _files.Where(x => x.container.id == c1.id && x.container.accountID == c1.accountID).FirstOrDefault();
          while(ff != null)
            {
              _files.Remove(ff);
              ff = _files.Where(x => x.container.id == c1.id && x.container.accountID == c1.accountID).FirstOrDefault();
            }
        }
    }

    public void delete(FreezeFile file)
    {
      if (file.container != null)
        {
          var conts = _containers
            .Where(x => x.accountID==file.container.accountID && x.id == file.container.id)
            ;
          foreach(var c in conts)
            {
              var fs = c.files.Where(x => x.fileID == file.fileID).ToList();
              foreach(var f1 in fs) { c.files.Remove(f1); }
            }
        }

      var fs1 = _files
          .Where(    x => x.fileID== file.fileID 
                 && (file.container != null 
                      ? x.container == null
                      : x.container.id == file.container.id && x.container.accountID== file.container.accountID))
          .ToList();
      foreach(var f in fs1) { _files.Remove(f); }
    }

    public IReadOnlyList<FreezeFile> getContainer(long accountID, string id, string name)
    {
      var cont = _containers.Where(x => x.accountID == accountID 
                  && (  (!string.IsNullOrWhiteSpace(id) && x.id == id)
                      || (string.IsNullOrWhiteSpace(id) && string.Compare(x.name, name, true) == 0)))
                      .FirstOrDefault();

      if (cont == null) { return new FreezeFile[0]{}; }
      return cont.files;
    }

    public IReadOnlyList<Container> getContainers(long accountID)
    {
      var cont = _containers.Where(x => x.accountID == accountID).ToList();
      return cont;
    }

    public IReadOnlyList<FreezeFile> getdir(string folder)
    {
      var lst = 
        from f in _files 
        where CultureInfo.InvariantCulture.CompareInfo.IsPrefix(f.path, folder, CompareOptions.IgnoreCase)
        select f;

      return lst.ToArray();
    }

    public void save(string file)
    {
      var strm = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read);

      var xdr = _serializerMake();
      var ucx = new FileCacheXml();

      ucx.containers.AddRange(_containers);
      xdr.Serialize(strm, ucx);
      strm.Close();
      strm.Dispose();
      strm = null;
      xdr = null;
    }

    public void load(string file)
    {
      var strm = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

      var fn = System.IO.Path.GetFileName(file);
      var path = file.Substring(0,file.Length - fn.Length);
      var db = CacheDBContext.Build(path);

      foreach(var f in this._files)
        {
          var f1 = db.FreezeFiles.Where(x => x.fileID == f.fileID).FirstOrDefault();
          if (f1 == null)
            {
              f1 = new FreezeFile
                {
                  path = f.path
                  ,mimeType = f.mimeType
                  , storedHash = f.storedHash
                  , localHash = f.localHash
                  , modified = f.modified
                  ,uploaded = f.uploaded
                  , fileID = f.fileID
                  , containerID = f.containerID
                  , serviceInfo = f.serviceInfo
                  , enchash = f.enchash
                };
              db.FreezeFiles.Add(f1);
              db.SaveChanges();
            }
        }
    }

    private XmlSerializer _serializerMake() 
    { return new XmlSerializer(typeof(FileCacheXml), new Type[] { typeof(FreezeFile), typeof(Hash)}); }
  }
}

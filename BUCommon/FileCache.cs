using System;
using System.Collections.Generic;
using System.Linq;

namespace BUCommon
{
  using System.Globalization;
  using System.IO;
  using Microsoft.EntityFrameworkCore;

  public class FileCache
  {
    
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


      var fn = System.IO.Path.GetFileName(file);
      var path = file.Substring(0,file.Length - fn.Length);
      var db = CacheDBContext.Build(path);


    }

    public void load(string file)
    {
      /* migrate to .json */
      if (string.Compare(".xml", System.IO.Path.GetExtension(file), true) == 0)
        {
          var oldStorage = 

        }

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

    }

    private void _writeFiles(CacheDBContext db)
    {
      foreach(var f in this._files)
        {
          var f1 = (
            from cf in db.Files
            join c1 in db.Containers on cf.containerID equals c1.id
            where cf.fileID == f.fileID && c1.containerID == f.containerID
            select cf
            )
            .FirstOrDefault();
          
          if (f1 == null)
            {
              Models.Container c = _makeContainer(db, f.container);

              f1 = new Models.ContFile
                {
                  path = f.path
                  ,mimeType = f.mimeType
                  , fileID = f.fileID
                  , containerID = c.id
                };
              db.Files.Add(f1);
              db.SaveChanges();
            }
          
          if (f1.path != f.path) { f1.path = f.path; }
          if (f1.mimeType != f.mimeType) { f1.mimeType = f.mimeType; }
          if (f1.modified != f.modified) { f1.modified = f.modified; }
          if (f1.uploaded != f.uploaded) { f1.uploaded = f.uploaded; }
          if (f1.serviceInfo != f.serviceInfo) { f1.serviceInfo = f.serviceInfo; }
          if (f1.enchash != f.enchash) { f1.enchash = f.enchash; }

          Models.Hash sh = _makeHashRec(db, f.storedHash);
          Models.Hash lh = _makeHashRec(db, f.localHash);
          f1.storedHashID = sh?.id;
          f1.localHashID = lh?.id;
          
          var e = db.Entry(f1);
          if (e.State == EntityState.Modified || e.State == EntityState.Added)
            {
              db.SaveChanges();
            }
        }
    }

    private Models.Hash _makeHashRec(CacheDBContext db, Hash h)
    {
      Models.Hash sh = null;
      if (h != null && !string.IsNullOrWhiteSpace(h.type) && !string.IsNullOrWhiteSpace(h.base64))
        {
          sh = db.Hashes.Where(x => x.base64 == h.base64 && x.type == h.type).FirstOrDefault();
          if (sh == null )
            {
              sh = new Models.Hash { base64=h.base64, type=h.type };
              db.Hashes.Add(sh);
              db.SaveChanges();
            }
        }
      
      return sh;
    }
    private Models.Container _makeContainer(CacheDBContext db, Container c)
    {
      Models.Container c1 = null;
      c1 = db.Containers.Where(x => x.containerID == c.id && x.type == c.type && x.accountID == c.accountID).FirstOrDefault();
      if (c1 == null)
        {
          c1 = new Models.Container 
            {
              type=c.type
              , accountID = c.accountID
              , name=c.name
              , containerID = c.id
            };
          db.Containers.Add(c1);
          db.SaveChanges();
        }
      
      return c1;
    }

  }
}

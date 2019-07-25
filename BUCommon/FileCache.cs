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
    private CacheDBContext _db;
    private List<Container> _containers;

    public FileCache() { _containers = new List<Container>(); }

    public void add(FreezeFile ff)
    {
      Models.ContFile oldf = null;
      
      if (ff.container == null)
        {
          oldf = _db.Files
            .Where(x =>    (x.container == null)
                        && (   (!string.IsNullOrWhiteSpace(x.fileID) && x.fileID == ff.fileID) 
                            || (string.IsNullOrWhiteSpace(x.fileID) && x.path == ff.path)
                            )
                   )
            .FirstOrDefault();
        }
      else
        {
          oldf = _db.Files
            .Where(x =>    (   x.container != null 
                               && (   x.accountID == ff.container.accountID 
                                   && x.containerID == ff.container.id))
                           )
                        && (   (!string.IsNullOrWhiteSpace(x.fileID) && x.fileID == ff.fileID) 
                            || (string.IsNullOrWhiteSpace(x.fileID) && x.path == ff.path)
                            )
                   )
            .FirstOrDefault();          
        }

      if (oldf == null) 
        {
          var dbf = new Models.ContFile();
          
          if (ff.container != null)
            {
              dbf.accountID = ff.container.accountID;
              dbf.containerID = ff.container.containerID;
            }
          
          dbf.fileID = ff.fileID;
          dbf.path = ff.path;

          oldf = _db.Files.Add(ff);
        }
      
      if (oldf.enchash != ff.enchash) { oldf.enchash = ff.enchash; }
      if (oldf.mimeType != ff.mimeType) { oldf.mimeType = ff.mimeType; }
      if (ff.modified != oldf.modified) { oldf.modified = ff.modified; }
      if (oldf.serviceInfo != ff.serviceInfo) { oldf.serviceInfo = ff.serviceInfo; }
      if (oldf.uploaded != ff.uploaded) { oldf.uploaded = ff.uploaded; }

      if (ff.localHash != null)
        {
          var h = _makeHashRec(ff.localHash);
          if (h != null 
              && ( oldf.localHashID == 0 || oldf.localHashID != h.id))
            { oldf.localHashID = h.id; }
        }
      if (ff.storedHash != null)
        {
          var h = _makeHashRec(ff.storedHash);
          if (h != null
              && (oldf.storedHashID == 0 || oldf.storedHashID != h.id))
            { oldf.storedHashID = h.id; }
        }

      _db.SaveChanges();
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
      var fs1 = _files
          .Where(    x => x.fileID== file.fileID 
                 && (file.container != null 
                      ? x.container == null
                      : x.container.id == file.container.id && x.container.accountID== file.container.accountID))
          .ToList();
      if (fs1.Any())
        {
          foreach(var f in fs1) 
            { _db.Files.Remove(f); }
          _db.SaveChanges();
        }
    }

    public IReadOnlyList<FreezeFile> getContainer(long accountID, string id, string name)
    {
      var cont = _db.Containers.Where(x => x.accountID == accountID 
                  && (  (!string.IsNullOrWhiteSpace(id) && x.containerID == id)
                      || (string.IsNullOrWhiteSpace(id) && string.Compare(x.name, name, true) == 0)))
                      .FirstOrDefault();

      if (cont == null) { return new FreezeFile[0]{}; }

      var files = _db.Files
        .Where(x => x.containerID == cont.id)
        .Select(g => new FreezeFile { });
      return files.ToList();
    }

    public IReadOnlyList<Container> getContainers(long accountID)
    {
      var cont = _db.Containers.Where(x => x.accountID == accountID).ToList();
      return cont;
    }

    public IReadOnlyList<FreezeFile> getdir(string folder)
    {
      var lst = 
        from f in _db.Files
        where CultureInfo.InvariantCulture.CompareInfo.IsPrefix(f.path, folder, CompareOptions.IgnoreCase)
        select f;

      return lst.ToList();
    }

    public void save(string file)
    {
      /*
      var fn = System.IO.Path.GetFileName(file);
      var path = file.Substring(0,file.Length - fn.Length);
      var db = CacheDBContext.Build(path);

      */
    }

    public void load(string file)
    {
      var fn = System.IO.Path.GetFileName(file);
      var path = file.Substring(0,file.Length - fn.Length);
      _db = CacheDBContext.Build(path);

      /* migrate to .json */
      if (string.Compare(".xml", System.IO.Path.GetExtension(file), true) == 0)
        {
          var oldStorage = Version1_0Storage.Build(file);

          foreach(var c in oldStorage.containers)
            {
              add(c);
            }
        }
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

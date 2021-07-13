using System;
using System.Collections.Generic;
using System.Linq;

namespace BUCommon
{
  using System.Globalization;
  using System.IO;
  using Microsoft.EntityFrameworkCore;
  using Newtonsoft.Json;

  public class FileCache
  {
    public class ContList
    {
      public List<Container> containers {get;set;}
      public ContList() { this.containers = new List<Container>(); }
    }

    public static FileCache Load(string file)
    {
      var fn = System.IO.Path.GetFileName(file);
      var path = file.Substring(0,file.Length - fn.Length);
      var db = CacheDBContext.Build(path);
      var cache = new FileCache(db);

      /* migrate to .json */
      if (string.Compare(".xml", System.IO.Path.GetExtension(file), true) == 0)
        {
          Console.WriteLine("loading old files...");
          var oldStorage = Version1_0Storage.Build(file);

          foreach(var c in oldStorage.containers) 
            {
              Console.WriteLine("loading {0} {1} -> {2}", c.id, c.name, c.files.Count);
              cache.add(c); 
            }
        }
      else
        {
          if (System.IO.File.Exists(file))
            {
              var str = System.IO.File.ReadAllText(file);
              var containers = Newtonsoft.Json.JsonConvert.DeserializeObject<ContList>(str);
              foreach(var c in containers.containers)
                { cache.add(c); }
            }
        }
      
      return cache;
    }

    private CacheDBContext _db;
    private List<Container> _containers;

    public IReadOnlyList<Container> containers { get { return _containers; } }

    protected FileCache(CacheDBContext db)
    {
      _containers = new List<Container>();
      _db = db;
    }

    public void add(FreezeFile ff)
    {
      Models.ContFile oldf = null;
      
      if (ff.container == null)
        {
          oldf = _db.Files
            .Where(x =>    (!string.IsNullOrWhiteSpace(x.fileID) && x.fileID == ff.fileID)
                        || (string.IsNullOrWhiteSpace(x.fileID) && x.path == ff.path)
                   )
            .FirstOrDefault();
        }
      else
        {
          oldf = _db.Files
            .Where(x =>    (   x.accountID == ff.container.accountID 
                            && x.containerID == ff.container.id)
                        && (   (!string.IsNullOrWhiteSpace(x.fileID) && x.fileID == ff.fileID) 
                            || (string.IsNullOrWhiteSpace(x.fileID) && x.path == ff.path)
                            )
                   )
            .FirstOrDefault();          
        }

      if (oldf == null) 
        {
          oldf = new Models.ContFile();
          
          if (ff.container != null)
            {
              oldf.accountID = ff.container.accountID;
              oldf.containerID = ff.container.id;
            }
          
          oldf.fileID = ff.fileID;
          oldf.path = ff.path;

          _db.Files.Add(oldf);
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
      /*
      foreach(var f in _db.Files
                  .Where(x => x.containerID == oldc.id && x.accountID == oldc.accountID))
        {
          cont.files.Add(f);
          f.container = cont;
        }
      */

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
          var ff = _db.Files.Where(x => x.containerID == c1.id && x.accountID == c1.accountID).FirstOrDefault();
          while(ff != null)
            {
              _db.Files.Remove(ff);
              _db.SaveChanges();
              ff = _db.Files.Where(x => x.containerID == c1.id && x.accountID == c1.accountID).FirstOrDefault();
            }
        }
    }

    public void delete(FreezeFile file)
    {
      var fs1 = _db.Files
          .Where(    x => x.fileID== file.fileID 
                 && (file.container != null 
                      ? string.IsNullOrWhiteSpace(x.containerID)
                      : x.containerID == file.container.id && x.accountID== file.container.accountID))
          .ToList();
      if (fs1.Any())
        {
          foreach(var f in fs1) 
            { _db.Files.Remove(f); }
          _db.SaveChanges();
        }
    }

    public IQueryable<FreezeFile> getContainer(long accountID, string id, string name)
    {
      System.Func<Models.Hash, Hash> hashAct = (Models.Hash h) => h == null ? null : Hash.Create(h.type, h.base64);
      var cont = _containers.Where(x => 
                     (accountID == 0 || x.accountID == accountID)
                  && (string.IsNullOrWhiteSpace(id) || x.id == id)
                  && (string.IsNullOrWhiteSpace(name) || string.Compare(x.name, name, true) == 0)
                  )
                  .ToList();

      if (!cont.Any()) 
        { return (IQueryable<FreezeFile>)(new List<FreezeFile>()); }

      var files = 
        from f in _db.Files
        join h in _db.Hashes on f.localHashID equals h.id
          into fhg from f1 in fhg.DefaultIfEmpty()
        join h2 in _db.Hashes on f.storedHashID equals h2.id
          into fh2g from f2 in fh2g.DefaultIfEmpty()
        where cont.Where(c => f.containerID == c.id).Any()
        select new FreezeFile { 
            path=f.path
            ,mimeType = f.mimeType
            ,storedHash = hashAct(fhg.FirstOrDefault())
            , localHash = hashAct(fh2g.FirstOrDefault())
            , fileID=f.fileID
            , modified = f.modified
            , uploaded = f.uploaded
            , serviceInfo = f.serviceInfo
            , enchash = f.enchash
            , containerID = f.containerID
            };
      
      return files;
    }

    public IQueryable<FreezeFile> getContNoHash(long accountID, string id, string name)
    {
      System.Func<Models.Hash, Hash> hashAct = (Models.Hash h) => h == null ? null : Hash.Create(h.type, h.base64);
      var cont = _containers.Where(x => 
                     (accountID == 0 || x.accountID == accountID)
                  && (string.IsNullOrWhiteSpace(id) || x.id == id)
                  && (string.IsNullOrWhiteSpace(name) || string.Compare(x.name, name, true) == 0)
                  )
                  .ToList();

      if (!cont.Any()) 
        { return (IQueryable<FreezeFile>)(new List<FreezeFile>()); }

      var files = 
        from f in _db.Files
        where cont.Where(c => f.containerID == c.id).Any()
        select new FreezeFile { 
            path=f.path
            ,mimeType = f.mimeType
            ,storedHash = null
            , localHash = null
            , fileID=f.fileID
            , modified = f.modified
            , uploaded = f.uploaded
            , serviceInfo = f.serviceInfo
            , enchash = f.enchash
            , containerID = f.containerID
            };
      
      return files;
    }


    public IReadOnlyList<Container> getContainers(long accountID)
    {
      var cont = _containers.Where(x => x.accountID == accountID).ToList();
      return cont;
    }

    public IReadOnlyList<FreezeFile> getdir(string folder)
    {
      System.Func<Models.Hash, Hash> hashAct = (Models.Hash h) => h == null ? null : Hash.FromString(h.type, h.base64);

      var lst = 
        from f in _db.Files
        join h in _db.Hashes on f.localHashID equals h.id
          into fhg from f1 in fhg.DefaultIfEmpty()
        join h2 in _db.Hashes on f.storedHashID equals h2.id
          into fh2g from f2 in fh2g.DefaultIfEmpty()
        where CultureInfo.InvariantCulture.CompareInfo.IsPrefix(f.path, folder, CompareOptions.IgnoreCase)
        select new FreezeFile { 
            path=f.path
            ,mimeType = f.mimeType
            ,storedHash = hashAct(fhg.FirstOrDefault())
            , localHash = hashAct(fh2g.FirstOrDefault())
            , fileID=f.fileID
            , modified = f.modified
            , uploaded = f.uploaded
            , serviceInfo = f.serviceInfo
            , enchash = f.enchash
            , containerID = f.containerID
            };

      return lst.ToList();
    }

    public void save(string file)
    {
      var fn = System.IO.Path.GetFileName(file);
      var path = file.Substring(0,file.Length - fn.Length);
      /*
      var db = CacheDBContext.Build(path);
      */

      var lst = new ContList();
      foreach(var c in _containers)
        { lst.containers.Add(new Container { id=c.id, accountID=c.accountID, name=c.name, type=c.type, encType=c.encType}); }
      
      var str = Newtonsoft.Json.JsonConvert.SerializeObject(lst, Formatting.Indented);
      var wr = new System.IO.FileStream(file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
      var swr = new StreamWriter(wr);
      swr.Write(str);
      swr.Close();
      swr.Dispose();
    }

    private Models.Hash _makeHashRec(Hash h)
    {
      Models.Hash sh = null;
      if (h != null && !string.IsNullOrWhiteSpace(h.type) && !string.IsNullOrWhiteSpace(h.base64))
        {
          sh = _db.Hashes.Where(x => x.base64 == h.base64 && x.type == h.type).FirstOrDefault();
          if (sh == null )
            {
              sh = new Models.Hash { base64=h.base64, type=h.type };
              _db.Hashes.Add(sh);
              _db.SaveChanges();
            }
        }
      
      return sh;
    }
  }
}

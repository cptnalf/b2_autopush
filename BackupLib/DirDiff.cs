using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackupLib
{
  using BUCommon;
  using System.IO;

  public enum DiffType
  {
    unknown
    ,same
    ,deleted
    ,created
    ,updated
  }

  public class FileDiff
  {
    public FreezeFile local {get;set; }
    public FreezeFile remote {get;set;}
    public DiffType type {get;set; }
  }

  /// <summary>
  /// take 2 directory listings and produce a list of files that are different.
  /// </summary>
  /// <remarks>
  /// this compares both the date and the time of the changes.
  /// </remarks>
  public class DirDiff
  {
    public bool usehash {get;set;}
    public string privateKey {get;set;}
    public string pathRoot {get;set;}
    public int maxTasks {get;set;}

    public IReadOnlyList<FileDiff> compare(IReadOnlyList<FreezeFile> local, IReadOnlyList<FreezeFile> provider)
    {
      List<FileDiff> files = new List<FileDiff>();

      maxTasks = IOUtils.DefaultTasks(maxTasks);

      var dels = 
        from pf in provider 
        where local.Where((x) => string.Compare(x.path, pf.path, true) == 0).Any() == false
        select new FileDiff { local=null, remote=pf, type=DiffType.deleted };

      files.AddRange(dels);

      var creates =
        from lf in local
        where provider.Where((x) => string.Compare(x.path, lf.path, true) == 0).Any() == false
        select new FileDiff { local=lf, remote=null, type= DiffType.created };

      files.AddRange(creates);
      
      IEnumerable<FileDiff> updates = null;
      if (usehash)
        {
          var lst = 
            from lf in local 
            join pf in provider on lf.path equals pf.path
            select new FileDiff{local=lf, remote=pf};

          byte[] keyfile;
          {
            var kt = new MemoryStream();
            var fs = new FileStream(privateKey, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var len = BUCommon.IOUtils.WriteStream(fs,kt).Result;
            fs.Close();
            fs.Dispose();
            fs = null;

            keyfile = kt.ToArray();
          }

          var tasks = Parallel.ForEach(lst
        ,new ParallelOptions { MaxDegreeOfParallelism=maxTasks}
        ,() => 
        {
          var sr = new StreamReader(new MemoryStream(keyfile, 0, keyfile.Length, false, false));
          var rsa = KeyLoader.LoadRSAKey(sr);

          var fe1 = new FileEncrypt(rsa);
          sr = null;
          return new DiffProcessor.TLocalData { fe=fe1, auth=null };
        }
        ,(x,pls,tl) =>
        {
          if (string.IsNullOrWhiteSpace(x.remote.enchash))
            { x.type = x.local.modified > x.remote.uploaded ? DiffType.updated : DiffType.same; }
          else
            {
              x.type = DiffType.same;
              var fstream = x.local.readStream(pathRoot);
              var hash = tl.fe.hashContents("SHA1", fstream);

              var rhash = Convert.FromBase64String(x.remote.enchash);
              //var rhash = tl.fe.decBytes(bytes);
              for(int i=0; i < hash.raw.Length; ++i) 
                { if (hash.raw[i] != rhash[i]) { x.type = DiffType.updated; break; } }
            }

          return tl;
        }
            ,(tl) => { tl.fe = null; }
                );
        }
      else
        {
          updates =
            from lf in local
            join pf in provider on lf.path equals pf.path
            where lf.modified > pf.uploaded
            select new FileDiff { local=lf, remote=pf, type=DiffType.updated};
        }

      files.AddRange(updates);

      return files;
    }
  }
}

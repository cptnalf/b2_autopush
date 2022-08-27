using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
      var sorted = new Dictionary<string,FreezeFile[]>(OSFileEqualityComparer.Comparer());
      List<FileDiff> files = new List<FileDiff>();

      maxTasks = IOUtils.DefaultTasks(maxTasks);

      _addToDict(local, sorted);
      _addToDict(provider, sorted);

      byte[] keyfile;
      BlockingCollection<FileDiff> sameHashBC = null;
      List<Task> procHashes = null;
      if (usehash)
        {
          /* setup the shared things. */
          procHashes = new List<Task>();
          {
            var kt = new MemoryStream();
            var fs = new FileStream(privateKey, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var len = BUCommon.IOUtils.WriteStream(fs,kt).Result;
            fs.Close();
            fs.Dispose();
            fs = null;

            keyfile = kt.ToArray();
          }
          sameHashBC = new BlockingCollection<FileDiff>();

          var hashAct = () => {
            /* can't share these things between threads. */
            var sr = new StreamReader(new MemoryStream(keyfile, 0, keyfile.Length, false, false));
            var rsa = KeyLoader.LoadRSAKey(sr);

            var fe1 = new FileEncrypt(rsa);

            while(!sameHashBC.IsCompleted)
              {
                try {
                  var x = sameHashBC.Take();

                  if (string.IsNullOrWhiteSpace(x.remote.enchash))
                    { x.type = x.local.modified > x.remote.uploaded ? DiffType.updated : DiffType.same; }
                  else
                    {
                      x.type = DiffType.same;
                      var fstream = x.local.readStream(pathRoot);
                      var hash = fe1.hashContents("SHA1", fstream);

                      var rhash = Convert.FromBase64String(x.remote.enchash);
                      //var rhash = tl.fe.decBytes(bytes);
                      for(int i=0; i < hash.raw.Length; ++i) 
                        { if (hash.raw[i] != rhash[i]) { x.type = DiffType.updated; break; } }
                    }
                }
                catch(InvalidOperationException ioe)
                  {
                    
                  }
              }
          };

          for(int i=0; i < maxTasks; ++i)
            {
              var t = Task.Run(hashAct);
              procHashes.Add(t);
            }
        }

      foreach(var key in sorted.Keys)
        {
          var lst = sorted[key];
          if (lst[0] == null) { files.Add(new FileDiff { local=null, remote=lst[1], type=DiffType.deleted}); }
          else if (lst[1] == null) { files.Add(new FileDiff { local=lst[0], remote=null, type=DiffType.created}); }
          else
            {
              var fd = new FileDiff { local=lst[0], remote=lst[1] };
              files.Add(fd);
              /* base case. */
              if (usehash) { sameHashBC.Add(fd); }
              else
                { fd.type = fd.local.modified > fd.remote.uploaded ? DiffType.updated : DiffType.same; }
            }
        }
      
      if (usehash)
        {
          /* wait for everything to be done hashing. */
          sameHashBC.CompleteAdding();
          var t = Task.WhenAll(procHashes);
          t.Wait();
        }

      return files;
    }

    private void _addToDict(IReadOnlyList<FreezeFile> src, Dictionary<string,FreezeFile[]> dict)
    {
      foreach(var l in src)
        {
          FreezeFile[] lst;
          if (!dict.TryGetValue(l.path, out lst))
            {
              lst = new FreezeFile[2];
              dict.Add(l.path, lst);
            }
          
          if (l.container == null) { lst[0] = l; }
          else { lst[1] = l; }
        }
    }
  }
}

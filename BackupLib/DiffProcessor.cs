using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib
{
  using Path = System.IO.Path;
  using File = System.IO.File;
  using FileStream = System.IO.FileStream;
  using FileMode = System.IO.FileMode;
  using FileAccess = System.IO.FileAccess;
  using FileShare = System.IO.FileShare;
  using MemoryStream = System.IO.MemoryStream;
  using StreamReader = System.IO.StreamReader;

  public enum RunType
  {
    unknown
    ,upload
    ,download
  }

  public class DiffProcessor
  {
    internal class TLocalData
    {
      internal FileEncrypt fe {get;set;}
      internal object auth {get;set;}
    }
    
    private List<FileDiff> _diffs = new List<FileDiff>();

    public int maxTasks {get;set;}
    public Action<FileDiff,Exception> errorHandler {get;set;}
    public Action<FileDiff> progressHandler {get;set;}
    public BUCommon.Account account {get;set;}
    public BUCommon.Container container {get;set;}
    public string encKey {get;set;}
    public string root {get;set;}

    public DiffProcessor() { maxTasks=12; }

    public void add(FileDiff d) { _diffs.Add(d); }
    public void add(IEnumerable<FileDiff> ds) { _diffs.AddRange(ds); }

    public void run(RunType rt)
    {
      byte[] keyfile;
      {
        var kt = new MemoryStream();
        var fs = new FileStream(encKey, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var len = BUCommon.IOUtils.WriteStream(fs,kt).Result;
        fs.Close();
        fs.Dispose();
        fs = null;

        keyfile = kt.ToArray();
      }

      if (maxTasks <= 0 || maxTasks > 100) { maxTasks =0; }

      var service = account.service;
      var cache = service.fileCache;

      var tasks = Parallel.ForEach(_diffs
        ,new ParallelOptions { MaxDegreeOfParallelism=maxTasks}
        ,() => 
        {
          var sr = new StreamReader(new MemoryStream(keyfile, 0, keyfile.Length, false, false));
          var rsa = KeyLoader.LoadRSAKey(sr);

          var fe1 = new FileEncrypt(rsa);
          sr = null;
          var td = service.threadStart();
          return new TLocalData { fe=fe1, auth=td };
        }
        ,(x,pls,tl) =>
        {
          progressHandler?.Invoke(x);
          
          string path = string.Empty;
          
          FileStream filestrm = null;
          try {
            switch(rt)
              {
              case RunType.upload: 
                { 
                  path = x.local.path.Replace('/', Path.DirectorySeparatorChar); 
                  path = Path.Combine(root, path);
                  filestrm = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite|FileShare.Delete);
                  var hash = tl.fe.hashContents(filestrm);
                  /* since we're reading anyways, populate the file hash. */
                  x.local.localHash = hash;
                  cache.add(x.local);
                  
                  var memstrm = tl.fe.encrypt(filestrm);
                  service.uploadFile(tl.auth, container, x.local, memstrm);
                  memstrm.Dispose();
                  memstrm = null;
                  break; 
                }
              case RunType.download: 
                {
                  /* this really needs to check to see if we need to download it. 
                   * that will make it resumeable.
                   */
                  path = Path.Combine(root, x.remote.path.Replace('/', Path.DirectorySeparatorChar));

                  {
                    /* need to make sure the download directory parts exist before we download and save the file. */
                    var pathfname = Path.GetFileName(path);
                    var pathpart = path.Substring(0,path.Length - pathfname.Length);
                    System.IO.Directory.CreateDirectory(pathpart);
                  }

                  var strm = service.downloadFile(x.remote);
                  filestrm = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
                  tl.fe.decrypt(strm, filestrm);
                  strm.Dispose();
                  strm = null;

                  var hash = tl.fe.hashContents(filestrm);

                  x.remote.localHash = hash;
                  cache.add(x.remote);

                  filestrm.Close();
                  filestrm.Dispose();
                  filestrm = null;

                  break; 
                }
              }
          } 
          catch (Exception e)
            {
              errorHandler?.Invoke(x,e);
              throw new ArgumentException("Error processing file diff item.", e);
            }
          return tl;
        }
        ,x => { service.threadStop(x.auth); }
        );
    }
  }
}

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
    public BUCommon.IFileSvc service {get;set;}
    public BUCommon.Container container {get;set;}
    public string encKey {get;set;}
    public string root {get;set;}

    public DiffProcessor() { maxTasks=12; }

    public void add(FileDiff d) { _diffs.Add(d); }
    public void add(IEnumerable<FileDiff> ds) { _diffs.AddRange(ds); }

    public void run()
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

      var tasks = Parallel.ForEach(_diffs
        ,new ParallelOptions { MaxDegreeOfParallelism=maxTasks}
        ,() => 
        {
          var sr = new StreamReader(new MemoryStream(keyfile, 0, keyfile.Length, false, false));
          var rsa = KeyLoader.LoadRSAKey(sr);

          var fe1 = new FileEncrypt(rsa);
          sr = null;
          return new TLocalData { fe=fe1 };
        }
        ,(x,pls,tl) =>
        {
          progressHandler?.Invoke(x);

          string path = x.local.path.Replace('/', '\\');
          path = Path.Combine(root, path);
          FileStream filestrm = null;
          try {
              filestrm = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite|FileShare.Delete);

              var memstrm = tl.fe.encrypt(filestrm);
              service.uploadFile(container, x.local, memstrm);
              memstrm.Dispose();
              memstrm = null;
          } 
          catch (Exception e)
            {
              errorHandler?.Invoke(x,e);
              throw new ArgumentException("Error processing file diff item.", e);
            }
          return tl;
        }
        ,x => { }
        );
    }
  }
}

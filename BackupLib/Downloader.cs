using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib
{
  using BUCommon;
  using FileStream = System.IO.FileStream;
  using FileMode = System.IO.FileMode;
  using FileAccess = System.IO.FileAccess;
  using FileShare = System.IO.FileShare;
  using Path = System.IO.Path;

  public class Downloader
  {
    private Queue<FreezeFile> _files = new Queue<FreezeFile>();
    private List<FreezeFile> _current = new List<FreezeFile>();

    public int concurrent {get;set; }
    public string root {get;set; }

    public Action<string,int,int> progressFX {get;set; }
    public Action<Exception,string> errorFX {get;set; }

    public IFileSvc fileService {get;set; }

    public void run(IReadOnlyList<FreezeFile> files, FileEncrypt fe, Action<string,int,int> progressFx)
    {
      if (fileService == null) { throw new ArgumentNullException("fileservice"); }
      /* only do cuncurrent files at a time...
       * probably only stage one at a time
       * , and send x at a time.
       */
      
      FileStream filestrm = null;

      /* single file for now. */
      for(int x=0; x < files.Count; ++x)
        {
          var f = files[x];
          progressFx?.Invoke(f.path, x,files.Count);

          string path = f.path.Replace('/', '\\');
          path = Path.Combine(root, path);
          try {
            string file = Path.GetFileName(path);
            string pathparts = path.Substring(0,path.IndexOf(file));

            System.IO.Directory.CreateDirectory(pathparts);
            filestrm = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite|FileShare.Delete);
            var memstrm = fileService.downloadFile(f);
            byte[] bytes = BUCommon.IOUtils.ReadStream(memstrm).Result;
            fe.decrypt(bytes, filestrm);
            memstrm.Dispose();
            memstrm = null;
            
            //cache.add(f);
          } 
          catch (Exception e)
            { errorFX?.Invoke(e, path); }
        }
    }
  }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BUCommon;

namespace BackupLib
{
  using System.IO;

  public class LocalService : BUCommon.IFileSvc
  {
    private string _root;
    private FileCache _fileCache;

    public Account account {get;set;}
    public FileCache fileCache { get {return _fileCache; } set { _fileCache = value; } }

    public void authorize() { }

    public void delete(FreezeFile file) { File.Delete(file.fileID); }

    public Stream downloadFile(FreezeFile file)
    {
    throw new NotImplementedException();
    }

    public async Task<Stream> downloadFileAsync(FreezeFile file)
    {
      var fstrm = new FileStream(file.fileID, FileMode.Open, FileAccess.Read, FileShare.ReadWrite|FileShare.Delete);
      return await Task.Run(() => fstrm);
    }

    public IReadOnlyList<Container> getContainers()
    {
      var dirs = Directory.EnumerateDirectories(_root);
      var conts = new List<Container>();

      foreach(var d in dirs) 
        { conts.Add(new Container { id=Path.Combine(_root,d), name=d,type=null, accountID=account.id}); }

      return conts;
    }

    public IReadOnlyList<FreezeFile> getFiles(Container container)
    {
      var files = new List<FreezeFile>();

      foreach(var f in Directory.EnumerateFiles(container.id, "*", SearchOption.AllDirectories))
        {
          DateTime mt = File.GetLastWriteTimeUtc(f);
          /* since the other provides the stored hash, this should probably provide it too.. */

          var s = f.Substring(container.id.Length + 1).Replace('\\', '/'); /* include the directory separator. */
          files.Add(new FreezeFile{ container=container, path=s, fileID=Path.Combine(container.id, s), uploaded=mt});
        }

      return files;
    }

    public IReadOnlyList<FreezeFile> getVersions(Container container)
    {
      throw new NotImplementedException();
    }

    public void setParams(string connstr)
    {
      _root = connstr;
    }

    public void uploadFile(Container container, FreezeFile file, Stream contents)
    { var f = uploadFileAsync(container, file, contents).Result; }

    public async Task<FreezeFile> uploadFileAsync(Container container, FreezeFile file, Stream contents)
    {
      var path = string.Empty;
      if (string.IsNullOrWhiteSpace(file.fileID)) 
        { path = Path.Combine(container.id,file.path.Replace('/', '\\')); }
      else { path = file.fileID; }

      {
        string fn = Path.GetFileName(path);
        /* c:\tmp\foo\bar\ baz.jpg (7) 15+7 22 - 7 = 15 */
        string pp = path.Substring(0,path.Length - fn.Length);
        Directory.CreateDirectory(pp);
      }

      FileStream strm = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite| FileShare.Delete);
      int len = await IOUtils.WriteStream(contents, strm);
      contents.Seek(0, SeekOrigin.Begin);
      var hasha = System.Security.Cryptography.HashAlgorithm.Create("SHA256");
      var hash = hasha.ComputeHash(contents);

      var ff = new FreezeFile
        {
          fileID = path
          , container=container
          , path = file.path
          , uploaded = DateTime.UtcNow
          , modified = file.modified
        };

      {
        var sb = new StringBuilder();
        for(int i=0; i < hash.Length; ++i) { sb.AppendFormat("{0:x2}", hash[i]); }
        ff.storedHash = Hash.Create("SHA256", sb.ToString());
      }

      fileCache.add(ff);

      return ff;
    }
  }
}

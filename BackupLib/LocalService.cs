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

    public void setParams(string connstr)
    {
      _root = connstr;
    }

    public void authorize() { }
    
    public IReadOnlyList<Container> getContainers()
    {
      var dirs = Directory.EnumerateDirectories(_root);
      var conts = new List<Container>();

      foreach(var d in dirs) 
        { conts.Add(new Container { id=d, name=Path.GetFileName(d) ,type=null, accountID=account.id}); }

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

    public IReadOnlyList<FreezeFile> getVersions(Container container) { return getFiles(container); }

    public void delete(FreezeFile file) { var res = deleteAsync(file).Result; }
    public Task<FreezeFile> deleteAsync(FreezeFile file)
    {
      var res = Task.Run(() => { File.Delete(file.fileID); return file; } );
      return res;
    }

    /* so, this should place it in the recycle bin, but 
     * that's windows only, and i don't know really what
     * else to do...
     */
    public void remove(FreezeFile file) { delete(file); }
    public Task<FreezeFile> removeAsync(FreezeFile file)
    { return deleteAsync(file); }
    
    public object threadStart() { return null; }
    public void threadStop(object data) { }
    
    public Stream downloadFile(object data, FreezeFile file) { return downloadFileAsync(data, file).Result; }

    public async Task<Stream> downloadFileAsync(object data, FreezeFile file)
    {
      var fstrm = new FileStream(file.fileID, FileMode.Open, FileAccess.Read, FileShare.ReadWrite|FileShare.Delete);
      return await Task.Run(() => fstrm);
    }

    public FreezeFile uploadFile(object threadData, Container container, FreezeFile file, Stream contents, string enchash)
    { return uploadFileAsync(threadData, container, file, contents, enchash).Result; }

    public async Task<FreezeFile> uploadFileAsync(object threadData, Container container, FreezeFile file, Stream contents, string enchash)
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
      var hasha = (System.Security.Cryptography.HashAlgorithm)System.Security.Cryptography.CryptoConfig.CreateFromName("SHA256");
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

      return ff;
    }
  }
}

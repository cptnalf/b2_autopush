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

    public FileCache fileCache { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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
        { conts.Add(new Container { id=Path.Combine(_root,d), name=d,type=null}); }

      return conts;
    }

    public IReadOnlyList<FreezeFile> getFiles(Container container)
    {
      var files = new List<FreezeFile>();

      foreach(var f in Directory.EnumerateFiles(container.id, null, SearchOption.AllDirectories))
        {
          DateTime mt = File.GetLastWriteTimeUtc(Path.Combine(container.id, f));
          files.Add(new FreezeFile{ container=container, path=f, fileID=Path.Combine(container.id, f), modified=mt});
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

    public async Task<string> uploadFileAsync(Container container, FreezeFile file, Stream contents)
    {
      FileStream strm = new FileStream(file.fileID, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite| FileShare.Delete);
      int len = await IOUtils.WriteStream(contents, strm);

      return file.fileID;
    }
  }
}

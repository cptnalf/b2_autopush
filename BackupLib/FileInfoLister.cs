using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib
{
  using File = System.IO.File;
  
  public class FileInfoLister : IDisposable
  {
    private BUCommon.UploadCache _uc = new BUCommon.UploadCache();

    public void init()
    {
      string fname = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
      fname = System.IO.Path.Combine(fname, "b2appcache.xml");
      if (File.Exists(fname)) { _uc.read(fname); }
    }

    public IReadOnlyList<BUCommon.FreezeFile> get(string prefix)
    {
      var files = _uc.getdir(prefix);

      return files;
    }
    
#region IDisposable Support
    private bool disposedValue = false;

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
        {
          if (disposing)
            { 
              string fname = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
              fname = System.IO.Path.Combine(fname, "b2appcache.xml");
              _uc.write(fname);
            }

          disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~FileInfoLister() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      // GC.SuppressFinalize(this);
    }
#endregion
    }
}

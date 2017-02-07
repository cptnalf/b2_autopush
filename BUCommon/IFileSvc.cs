using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUCommon
{
  using MemoryStream = System.IO.MemoryStream;

  /// <summary>
  /// this is a low-level interface.
  /// all of these commands should contact the server.
  /// caching services for the various parts will be done upstream
  /// of this interface.
  /// </summary>
  public interface IFileSvc
  {
    void setParams(string connstr);
    void open();
    IReadOnlyList<Container> getContainers();
    IReadOnlyList<FreezeFile> getFiles(Container container);

    void uploadFile(Container container, FreezeFile file, byte[] contents);
    Task<string> uploadFileAsync(Container container, FreezeFile file, byte[] contents);

    MemoryStream downloadFile(FreezeFile file);
    Task<MemoryStream> downloadFileAsync(FreezeFile file);
  }
}

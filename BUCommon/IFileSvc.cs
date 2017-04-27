using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUCommon
{
  using Stream = System.IO.Stream;

  /// <summary>
  /// this is a low-level interface.
  /// all of these commands should contact the server.
  /// caching services for the various parts will be done upstream
  /// of this interface.
  /// </summary>
  public interface IFileSvc
  {
    FileCache fileCache {get;set;}
    Account account {get;set;}
    
    void setParams(string connstr);
    void authorize();
    IReadOnlyList<Container> getContainers();
    IReadOnlyList<FreezeFile> getFiles(Container container);
    IReadOnlyList<FreezeFile> getVersions(Container container);

    void delete(FreezeFile file);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="container"></param>
    /// <param name="file"></param>
    /// <param name="contents"></param>
    /// <remarks>
    /// when a file is uploaded to the service
    /// , it's expected that the implementor will update the cache with the new information
    /// that the uploade creates.
    /// </remarks>
    FreezeFile uploadFile(object threadData, Container container, FreezeFile file, Stream contents);
    Task<FreezeFile> uploadFileAsync(object threadData, Container container, FreezeFile file, Stream contents);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    /// <remarks>
    /// when a file is downloaded, any additional information the service provides
    /// should be added to the FreezeFile object and persisted in the cache
    /// so that it's available to the program going forward.
    /// </remarks>
    Stream downloadFile(FreezeFile file);
    Task<Stream> downloadFileAsync(FreezeFile file);

    object threadStart();
    void threadStop(object data);
  }
}

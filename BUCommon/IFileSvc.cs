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

    /// <summary>
    /// this function obliterates the file.
    /// </summary>
    /// <param name="file"></param>
    void delete(FreezeFile file);
    Task<FreezeFile> deleteAsync(FreezeFile file);

    /// <summary>
    /// this function marks it as deleted
    /// </summary>
    /// <param name="file"></param>
    /// <remarks>
    /// this function may have the same effect as the 'delete' function
    /// ,depending on the underlying service.
    /// (B2 has the concept of versions, so 'remove' will mark it as deleted
    ///  ,it will not show up in a regular 'get files'
    ///  , but versions will still exist.)
    /// </remarks>
    void remove(FreezeFile file);
    Task<FreezeFile> removeAsync(FreezeFile file);

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
    FreezeFile uploadFile(object threadData, Container container, FreezeFile file, Stream contents, string enchash);
    Task<FreezeFile> uploadFileAsync(object threadData, Container container, FreezeFile file, Stream contents, string enchash);

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
    Stream downloadFile(object threadData, FreezeFile file);
    Task<Stream> downloadFileAsync(object threadData, FreezeFile file);

    object threadStart();
    void threadStop(object data);
  }
}

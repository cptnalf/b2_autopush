using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommB2
{
  using B2Net;
  using File = System.IO.File;

  /* for grouping information see:
   * https://www.backblaze.com/b2/cloud-storage-pricing.html
   * and click on the developer link towards the bottom of the page.
   * 
   * Free, unlimited.
b2_delete_bucket
b2_delete_file_version
b2_hide_file
b2_get_upload_url
b2_upload_file
b2_start_large_file
b2_get_part_upload_url
b2_upload_part
b2_cancel_large_file
b2_finish_large_file
   * 
   * class B transactions 2500/day are free, then 0.004$ per 1000
b2_download_file_by_id
b2_download_file_by_name
b2_get_file_info
   * 
   * 
   * class C transactions, 2500/day are free, then 0.004$ per 1000
b2_authorize_account
b2_create_bucket
b2_list_buckets
b2_list_file_names
b2_list_file_versions
b2_update_bucket
b2_list_parts
b2_list_unfinished_large_files
b2_get_download_authorization
   * 
   * @TODO lots of lists of stuff :(
   * all of the above means this data needs to be cached locally so that an interface could be constructed to display it.
   * this will also help in estimating costs for operations.
   * 1) cache solid.
   * 2) settings (account list, public/private key locations, cache file locations)
   * 3) fix auth pattern and storage. 
   *    (maybe encrypt locally?) 
   *    includes re-auth in application after exception for bad/expired auth
   * 4) 
   * 
   */

  /// <summary>
  /// 
  /// </summary>
  public class Connection : BUCommon.IFileSvc
  {
    private B2Client _client;
    private B2Net.Models.B2Options _opts;
    private BUCommon.FileCache _cache;
    private BUCommon.AuthStorage _auth;

    public BUCommon.FileCache fileCache { get { return _cache;} set { _cache = value; } }
    public BUCommon.AuthStorage auth { get { return _auth; } set { _auth = value; } }

    public void setParams(string connstr)
    {
      var parts = BUCommon.FileSvcBase.ParseConnStr(connstr);
      var opts = new B2Net.Models.B2Options();
      opts.AccountId = parts[0];
      opts.ApplicationKey = parts[1];
      
      opts.AuthorizationToken = _auth["AuthorizationToken"];
      opts.DownloadUrl = _auth["DownloadUrl"];
      opts.ApiUrl = _auth["ApiUrl"];

      opts.AuthorizationToken = "<token>";
      opts.DownloadUrl = "https://f001.backblazeb2.com";
      opts.ApiUrl = "https://api001.backblazeb2.com";
      
      _client = new B2Client(opts);

      /*
      var blst = x.Buckets.GetList().Result;

      var bkt = blst.FirstOrDefault();
      var flst = x.Files.GetList(bkt.BucketId);
      */
    }

    /// <summary>
    /// populate the list of 
    /// </summary>
    public IReadOnlyList<BUCommon.Container> getContainers()
    {
     // if (string.IsNullOrWhiteSpace(_opts.AuthorizationToken)) { throw new ArgumentException("Open the connection first."); }
      var res = _client.Buckets.GetList().Result;
      
      List<BUCommon.Container> buckets = new List<BUCommon.Container>();
      foreach(var x in res)
        {
          var cb = new BUCommon.Container
            {
               id=x.BucketId
               , name=x.BucketName
               ,type=x.BucketType
            };
          
          buckets.Add(_cache.add(cb));
        }

      return buckets;
    }

    /// <summary>
    /// connect to b2.
    /// </summary>
    public void authorize() 
    {
      _opts = _client.Authorize().Result; 
      _auth.add("AuthorizationToken", _opts.AuthorizationToken);
      _auth.add("DownloadUrl", _opts.DownloadUrl);
      _auth.add("ApiUrl", _opts.ApiUrl);
    }

    public BUCommon.Container containerCreate(BUCommon.Account account, string name)
    {
      var res = _client.Buckets.Create(name, B2Net.Http.BucketTypes.allPrivate).Result;
      return new BUCommon.Container 
          { 
            accountID =account.id, account=account
            , id=res.BucketId, name=res.BucketName, type=res.BucketType 
          };
    }
    
    /// <summary>
    /// get the list of files in the specified bucket.
    /// </summary>
    /// <param name="bucketid"></param>
    /// <returns></returns>
    public IReadOnlyList<BUCommon.FreezeFile> getFiles(BUCommon.Container cont)
    {
      List<BUCommon.FreezeFile> list = new List<BUCommon.FreezeFile>();
      B2Net.Models.B2FileList files = null;
      string startfile = null;

      do {
        files = _client.Files.GetList(startfile, null, cont.id).Result;
        foreach(var f in files.Files)
          {
            var ff = new BUCommon.FreezeFile
              {
               path = f.FileName
               , uploaded = f.UploadTimestampDate
               , fileID = f.FileId
               , storedHash = BUCommon.Hash.Create("SHA1", f.ContentSHA1)
               , mimeType = f.ContentType
               ,container = cont
              };
            _cache.add(ff);
            list.Add(ff);
          }
        startfile = files.NextFileName;

      } while (startfile != null);

      return list;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cont"></param>
    /// <returns></returns>
    public IReadOnlyList<BUCommon.FreezeFile> getVersions(BUCommon.Container cont)
    {
      List<BUCommon.FreezeFile> list = new List<BUCommon.FreezeFile>();
      B2Net.Models.B2FileList files = null;
      string startfile = null;
      string startfileID = null;

      do {
        files = _client.Files.GetVersions(startfile, null, null, cont.id ).Result;
        startfile = files.NextFileName;
        startfileID = files.NextFileId;

        foreach(var f in files.Files)
          {
            var ff = new BUCommon.FreezeFile
              {
                path = f.FileName
               ,uploaded=f.UploadTimestampDate
               ,mimeType=f.ContentType
               ,fileID=f.FileId
               ,storedHash = BUCommon.Hash.Create("SHA1", f.ContentSHA1)
               ,container=cont
               ,serviceInfo=f.Action
              };
            list.Add(ff);
          }
      } while (startfile != null);

      return list;
    }
    
    public void delete(BUCommon.FreezeFile file) { var x = deleteAsync(file).Result; }

    public async Task<string> deleteAsync(BUCommon.FreezeFile file)
    {
      var res = await _client.Files.Hide(file.path, file.container.id);

      return res.FileId;
    }

    public void uploadFile(BUCommon.Container cont, BUCommon.FreezeFile file, System.IO.Stream contents)
    { var foo = uploadFileAsync(cont, file, contents).Result; }

    public async Task<string> uploadFileAsync(BUCommon.Container cont, BUCommon.FreezeFile file, System.IO.Stream contents)
    {
      DateTimeOffset dto = new DateTimeOffset(file.modified.ToUniversalTime());
      var millis = dto.ToUnixTimeMilliseconds();
      var argdic = new Dictionary<string,string>();
      argdic.Add("src_last_modified_millis", millis.ToString());
      byte[] bytes = await BUCommon.IOUtils.ReadStream(contents);

      var res = await _client.Files.Upload(bytes, file.path, cont.id, argdic);

      /* create another freezefile for the new bit. */
      var ff = new BUCommon.FreezeFile
        {
          fileID=res.FileId
          , container=cont
          , uploaded = res.UploadTimestampDate
          , storedHash = BUCommon.Hash.Create("SHA1", res.ContentSHA1)
          , mimeType = res.ContentType
          , path = res.FileName
          , modified =file.modified
        };

      _cache.add(ff);

      return ff.fileID;
    }

    public async Task<System.IO.Stream> downloadFileAsync(BUCommon.FreezeFile file)
    {
      if (string.IsNullOrWhiteSpace(file.fileID)) { throw new ArgumentNullException("fileID"); }

      var task = await _client.Files.DownloadById(file.fileID);
      
      file.uploaded = task.UploadTimestampDate;
      file.storedHash = BUCommon.Hash.Create("SHA1", task.ContentSHA1);
      file.mimeType = task.ContentType;
      file.path = task.FileName;
      if( task.FileInfo != null)
        {
          string lastmillis = null;
          /*x-bz-info-src_last_modified_millis */
          if (task.FileInfo.TryGetValue("x-bz-info-src_last_modified_millis", out lastmillis))
            {
              var dto = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(lastmillis));
              file.modified = dto.DateTime.ToLocalTime();
            }
        }

      return new System.IO.MemoryStream(task.FileData);
    }

    public System.IO.Stream downloadFile(BUCommon.FreezeFile file) { return downloadFileAsync(file).Result; }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommB2
{
  using B2Net;
  using File = System.IO.File;

  public class ContainerB2 : BUCommon.Container
  {
    internal string type {get;set; }
  }

  public class Connection : BUCommon.IFileSvc
  {
    private B2Client _client;
    private B2Net.Models.B2Options _opts;

    public void setParams(string connstr)
    {
      var parts = BUCommon.FileSvcBase.ParseConnStr(connstr);
      var opts = new B2Net.Models.B2Options();
      opts.AccountId = parts[0];
      opts.ApplicationKey = parts[1];

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
      if (string.IsNullOrWhiteSpace(_opts.AuthorizationToken)) { throw new ArgumentException("Open the connection first."); }
      var res = _client.Buckets.GetList().Result;
      
      List<BUCommon.Container> buckets = new List<BUCommon.Container>();
      foreach(var x in res)
        {
          var cb = new ContainerB2
            {
               id=x.BucketId
               , name=x.BucketName
               ,type=x.BucketType
            };
          buckets.Add(cb);
        }

      return buckets;
    }

    /// <summary>
    /// connect to b2.
    /// </summary>
    public void open() { _opts = _client.Authorize().Result; }

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
              };
            list.Add(ff);
          }
        startfile = files.NextFileName;

      } while (startfile != null);

      return list;
    }

    public void uploadFile(BUCommon.Container cont, BUCommon.FreezeFile file, byte[] contents)
    { var foo = uploadFileAsync(cont, file, contents).Result; }

    public async Task<string> uploadFileAsync(BUCommon.Container cont, BUCommon.FreezeFile file, byte[] contents)
    {
      DateTimeOffset dto = new DateTimeOffset(file.modified.ToUniversalTime());
      var millis = dto.ToUnixTimeMilliseconds();
      var argdic = new Dictionary<string,string>();
      argdic.Add("src_last_modified_millis", millis.ToString());
      var res = await _client.Files.Upload(contents, file.path, cont.id, argdic);

      file.fileID = res.FileId;
      file.storedHash = BUCommon.Hash.Create("SHA1", res.ContentSHA1);
      file.uploaded= res.UploadTimestampDate;

      return file.fileID;
    }

    public async Task<System.IO.MemoryStream> downloadFileAsync(BUCommon.FreezeFile file)
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

    public System.IO.MemoryStream downloadFile(BUCommon.FreezeFile file) { return downloadFileAsync(file).Result; }
  }
}

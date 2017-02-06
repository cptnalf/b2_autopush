using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommB2
{
  using B2Net;

  public class Connection
  {
    /// <summary>
    /// build a connection object for connecting to B2 
    /// </summary>
    /// <param name="accountID"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static Connection Generate(string accountID, string key)
    {
      var opts = new B2Net.Models.B2Options();
      opts.AccountId = accountID;
      opts.ApplicationKey = key;

      var conn = new Connection();
      conn._client = new B2Client(opts);

      /*
      var blst = x.Buckets.GetList().Result;

      var bkt = blst.FirstOrDefault();
      var flst = x.Files.GetList(bkt.BucketId);
      */

      return conn;
    }

    private B2Client _client;
    private B2Net.Models.B2Options _opts;

    private List<B2Net.Models.B2Bucket> _buckets = new List<B2Net.Models.B2Bucket>();
    
    /// <summary>
    /// populate the list of 
    /// </summary>
    public void getBuckets()
    {
      var res = _client.Buckets.GetList().Result;
      _buckets.AddRange(res);
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
    public IReadOnlyList<BUCommon.FreezeFile> getList(string bucketid)
    {
      List<BUCommon.FreezeFile> list = new List<BUCommon.FreezeFile>();
      B2Net.Models.B2FileList files = null;
      string startfile = null;

      do {
        files = _client.Files.GetList(startfile, null, bucketid).Result;
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
  }
}

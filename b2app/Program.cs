using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace b2app
{
  class Program
  {
    private RSACryptoServiceProvider _rsa;
    private string _encrFolder = "c:\\tmp\\enc";
    private string _decrFolder = "c:\\tmp\\dec";
    private string _srcFolder = "c:\\tmp\\src";
    private CspParameters _cspp = new CspParameters(1);

    static void Main(string[] args)
    {
      /*
       * process:
       * load local files from specified place
       * try cached remote store list first
       * if cached is not available (or x days old?)
       *  contact remote store for file list (with details)
       * 
       * compare local file list to remote file list
       *  - use upload and last-modified times to also observe changes
       *  - maybe use a file-change service to record changes to look at?
       * 
       * generate change list
       * 
       * get asymetric key information
       * test asymetric key information
       * 
       * process updates
       * process deletes
       * process adds
       * 
       * upload process:
       *  - get file content hash
       *  - get file update time/date
       *  - generate file key
       *  - encrypt file key
       *  - encrypt file (memory?)
       *  - sign encrypted content or hash of encrypted content.
       *  - upload in-memory encrypted part
       *  - add file information to local cache, persist cache to disk.
       * 
       */

      /* local cache should start as a persisted version of the FreezeFile list
       * created from the downloaded portion.
       * 
       * seems like a compressed xml or json would be good?
       */

      /* 
       */
#if dob2
      var opts = new B2Net.Models.B2Options();
      opts.AccountId = "";
      opts.ApplicationKey = "";

      var x = new B2Net.B2Client(opts);
      var autht = x.Authorize().Result;
      
      var blst = x.Buckets.GetList().Result;

      var bkt = blst.FirstOrDefault();
      var flst = x.Files.GetList(bkt.BucketId);
#endif
      CommB2.Connection conn = CommB2.Connection.Generate("", "");

      /* list dir.
       * put files into various parts.
       */
      /*
      var aes = Aes.Create();
      var enc = aes.CreateEncryptor(aes.Key,aes.IV);

      var rsa = RSA.Create();
      var rsaparams = new RSAParameters();
      rsa.ImportParameters(rsaparams);
      */

      var p = new Program();
      //p._loadKey("c:\\tmp\\id_rsa_1_pub", string.Empty);
    }
  }
}

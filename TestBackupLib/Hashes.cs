using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestBackupLib
{
  [TestClass]
  public class Hashes
  {
    [TestMethod]
    public void B2SHA1()
    {
      var destHash = "eeea523c2b2dd9415c657556e45f73be993d7979";
      var hash = BUCommon.Hash.FromString("SHA1", destHash);

      var fe = new BackupLib.FileEncrypt(null);
      BUCommon.Hash filehash = null;
      byte[] filebytes = null;
      {
        var strm = new System.IO.FileStream(@"C:\tmp\b2test\cont1\2016\1231-newyears\DSC06560.ARW", System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
        filehash = fe.hashContents("SHA1", strm);

        strm.Seek(0, System.IO.SeekOrigin.Begin);
        var mstr = new System.IO.MemoryStream();
        byte[] buff = new byte[8192];
        int len = 0;
        while(true)
          {
            len = strm.Read(buff, 0, buff.Length);
            if (len > 0) { mstr.Write(buff, 0, len); }
            if (len == 0) { break;}
          }

        strm.Dispose();
        strm = null;

        filebytes = mstr.ToArray();
        mstr.Dispose();
        mstr = null;
      }
      
      string b2nethash = B2Net.Utilities.GetSHA1Hash(filebytes);


      Assert.AreEqual(hash.base64, filehash.base64);
      
    }

    [TestMethod]
    public void ECDSATest()
    {
      var ecdsa = System.Security.Cryptography.ECDsa.Create();
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestBackupLib
{
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using System.IO;

  [TestClass]
  public class Class1
  {
    public TestContext tc {get;set; }

    public Class1()
    {
    }

    [TestMethod]
    public void TestLocalLister()
    {
      var ll = new BackupLib.LocalLister();
      var fl = ll.getList("c:\\tmp");

      Assert.IsNotNull(fl);
      Assert.IsTrue(fl.Count > 0);
    }

    [TestMethod]
    public void TestDirDiff()
    {
      var ll = new BackupLib.LocalLister();
      var fl = ll.getList("c:\\tmp");
      
      var dd = new BackupLib.DirDiff();
      var cmpres = dd.compare(fl, fl);
      Assert.IsNotNull(cmpres);
      Assert.IsTrue(cmpres.Count == 0);
    }

    [TestMethod]
    public void TestDirDiffD()
    {
      var ll = new BackupLib.LocalLister();
      var fl = ll.getList("c:\\tmp");
      
      var fl2 = new List<BackupLib.FreezeFile>(fl);
      fl2.Add(new BackupLib.FreezeFile { path="blarg/foo.jpg" });

      var dd = new BackupLib.DirDiff();
      var cmpres = dd.compare(fl, fl2);
      Assert.IsNotNull(cmpres);
      Assert.IsTrue(cmpres.Count > 0);

      var x = cmpres.FirstOrDefault();
      Assert.IsNotNull(x);
    }

    [TestMethod]
    public void TestFileEncrypt()
    {
      var strm = new MemoryStream();
      byte[] bytes = System.Text.Encoding.UTF8.GetBytes(Encrypting._PUBKEY);
      strm.Write(bytes, 0, bytes.Length);
      strm.Seek(0, SeekOrigin.Begin);
      var sr = new StreamReader(strm);
      var rsa = BackupLib.KeyLoader.LoadRSAKey(sr);

      BackupLib.FileEncrypt enc = new BackupLib.FileEncrypt(rsa);
      var srcfile = new FileStream("c:\\tmp\\foo.txt"/*"c:\\temp\\dumps\\ttirdreport.20170116_134248.xml"*/, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      var encbytes = enc.encrypt(srcfile);
      
      /* persist the encrypted file. */
      var destfile = new FileStream("c:\\tmp\\encfile.enc", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
      encbytes.WriteTo(destfile);
      destfile.Flush();
      encbytes.Seek(0, SeekOrigin.Begin);

      destfile.Dispose();
    }
   
    [TestMethod]
    public void TestFileDecrypt()
    {
      MemoryStream strm;
      StreamReader sr;
      byte[] bytes;
      var encbytes = new MemoryStream();
      var destfile = new FileStream("c:\\tmp\\encfile.enc", FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
      destfile.CopyTo(encbytes);
      destfile.Dispose();
      
      destfile = new FileStream("c:\\tmp\\encfile.ok", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
      encbytes.Seek(0, SeekOrigin.Begin);

      strm = new MemoryStream();
      bytes = System.Text.Encoding.UTF8.GetBytes(Encrypting._PRIVATEKEY);
      strm.Write(bytes, 0, bytes.Length);
      strm.Seek(0, SeekOrigin.Begin);
      sr = new StreamReader(strm);
      var rsa1 = BackupLib.KeyLoader.LoadRSAKey(sr);
      BackupLib.FileEncrypt dec = new BackupLib.FileEncrypt(rsa1);
      dec.decrypt(encbytes,destfile);
    }
  }
}

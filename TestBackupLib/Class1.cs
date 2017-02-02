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
    public void TestFileHeader()
    {
      var fh = new BackupLib.FileEncrypt.FileHeader();
      fh.algo = "Flarg"; /* 9 */
      fh.hash = new byte[] { 1, 2, 3,4, 5}; /* 9 */
      fh.hashname = "SHA256"; /* 10 */
      fh.key = new byte[] { 9, 8, 7,6,5}; /* 9 */
      fh.iv = new byte[] { 12, 15, 19, 22 }; /* 8 */

      var headerbytes = fh.toBytes();
      Assert.IsNotNull(headerbytes);
      Assert.AreEqual(45, headerbytes.Length);
      
      var fh1 = BackupLib.FileEncrypt.FileHeader.Create(headerbytes);
      Assert.AreEqual(fh.algo, fh1.algo);
      CollectionAssert.AreEqual(fh.hash, fh1.hash);
      Assert.AreEqual(fh.hashname, fh1.hashname);
      CollectionAssert.AreEqual(fh.key, fh1.key);
      CollectionAssert.AreEqual(fh.iv, fh1.iv);
    }

    private System.Security.Cryptography.RSA _loadRSA(string keycontents)
    {
      var strm = new MemoryStream();
      var bytes = System.Text.Encoding.UTF8.GetBytes(Encrypting._PRIVATEKEY);
      strm.Write(bytes, 0, bytes.Length);
      strm.Seek(0, SeekOrigin.Begin);
      var sr = new StreamReader(strm);
      var rsa1 = BackupLib.KeyLoader.LoadRSAKey(sr);
      return rsa1;
    }
    
    private const string _ENCRYPTED = "AAEAAIDR3qDViTSVwZSfdkOH+fv0f/k2oheziAnT1KeVtjGxrw8vKcbc61uh1SUuK8neW2ZpaZYw11j+9Y/TdKxVheubHPO2ofHtRGm/xR19e9ptwHSs9Kd5eBRaIWdNhYCC5L+4ujC40TSRIewRMgjWfMOGZUTx0ajxtL5FnnXN3OfqjLSoQazxEDQt888AwKHcoNb/Ra/WTKyfBAavU8Q8j0LDEGjeSiDNQsbK1TBAGcD/1zB7FP5MThexKBi8bL9Bhk4/1AVmc3hF7Qd+fj2YcJ7NgoedNSae4myWAHKfkcc+h9ovBfNn8gGPvuBILmIn7zX0f0spZ0TmteeqNpa7dRZDI4UMXtnS4vNfy67KQh+yU8X3N7/ue6n80P7a3Db4lNW08VcXncRV2yKPNH+6EnOi251FrgYYO1+430VcVAyd1XhUPhLcjyRcqffRaYwoDHKEkAu/CgPahGv/kJM1N8fgUJ6XvYYjnZCjkoGguolnjpFsJpCCtbSWwlx94R0xWwFds3aOhN9gaAG8Wuz2i+qWyLQt9YfCEPDDIO3EEaYltyIDtj0DHR9neFEeXvRg5Jy0qpJSFd+eDy/wKFAfuMqjZt5Ri8wKn9jqJd74UQsX8zvY3MB0gAfXTSRooPYSyCujojmxFSx/2QHk0qMJ9zoD+TufEG29RmsBjD8yvURZWG3534dTYQJs+eHZao7j/miAvA90cvtVfGn5+TMnZbgkFcIzFRRPVi6LqLchPT7jYkHhxvOeVKdu4lCDnXslxTsXPAUh2+7JQ2wH95w/6HcciWb4mJ7HJcD6VIpcC91OJyFxwKVaulsnVZZGSLuD5pG66qZ+m5RfL3JFwIfRTvju753jgun/oxTgOuljcTlbPsFdgin+mfDg/opmH6Ao4zTcsClr1IW8ubfQA5jnBFtfgS3ZgF85Vme+gldyV/6rwp9EU6zloCfCyvHL1vmK6g==";

    [TestMethod]
    public void TestFileEncrypt()
    {
      var rsa = _loadRSA(Encrypting._PUBKEY);

      BackupLib.FileEncrypt enc = new BackupLib.FileEncrypt(rsa);
      //var srcfile = new FileStream("c:\\tmp\\foo.txt"/*"c:\\temp\\dumps\\ttirdreport.20170116_134248.xml"*/, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      var srcfile = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Encrypting._PUBKEY));
      var encbytes = enc.encrypt(srcfile);
      
      /* persist the encrypted file. */
      var destfile = new FileStream("c:\\tmp\\encfile.enc", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
      encbytes.WriteTo(destfile);
      destfile.Flush();
      encbytes.Seek(0, SeekOrigin.Begin);

      destfile.Dispose();

      var b64 = Convert.ToBase64String(encbytes.ToArray());
    }
   
    [TestMethod]
    public void TestFileDecrypt()
    {
      var encbytes = new MemoryStream(Convert.FromBase64String(_ENCRYPTED));
      /*
      var destfile = new FileStream("c:\\tmp\\encfile.enc", FileMode.Open, FileAccess.Read, FileShare.Read);
      destfile.CopyTo(encbytes);
      destfile.Dispose();
      */
      
      var destfile = new FileStream("c:\\tmp\\encfile.ok", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
      encbytes.Seek(0, SeekOrigin.Begin);

      var rsa1 = _loadRSA(Encrypting._PRIVATEKEY);

      BackupLib.FileEncrypt dec = new BackupLib.FileEncrypt(rsa1);
      dec.decrypt(encbytes,destfile);
    }
  }
}

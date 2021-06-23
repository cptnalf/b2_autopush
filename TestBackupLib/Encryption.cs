using System;
using System.Diagnostics;
using NUnit.Framework;

using System.IO;

namespace TestBackupLib
{
  [TestFixture]
  [Category("TEST_ENCRYPT")]
  public class Encryption
  {
    internal const string testSRCPATH = "/r1/photos/source/alpha7/2019/1026-oregon/DSC00628.ARW";
    internal const string testDESTDIR = "/data2/temp/";
    internal const string testKEY = "./test_rsa";
    internal const string AGEBASE = "/home/chiefengineer/releases/age";
    internal const string AGE_RECIPIENT = "/data2/temp/test_recipient.txt";
    internal const string AGE_KEY = "/data2/temp/age_key.txt";

    internal const string BASIC_DESTNAME = "b2app_dec.test";
    internal const string AGE_DESTNAME = "b2app_dec.age.test";

    [Test]
    public void Basic()
    {
      var kl = BackupLib.KeyLoader.LoadRSAKey(string.Format("{0}.pub", testKEY));

      var fe = new BackupLib.FileEncrypt(kl);
      var fstream = _setupInFile();
      var dest = fe.encrypt(fstream);
      
      fstream.Dispose();
      fstream = null;

      kl = BackupLib.KeyLoader.LoadRSAKey(string.Format("{0}.pem", testKEY));
      fe = new BackupLib.FileEncrypt(kl);
      var test_out = _setupOutFile(BASIC_DESTNAME);
      fe.decrypt(dest, test_out);

      test_out.Dispose();
      test_out = null;
      dest = null;
      Assert.That(true);
    }

    [Test]
    public void BasicAge()
    {
      var ae = new BackupLib.Age.AgeEncrypt();
      ae.AgePath = AGEBASE;
      ae.ReceipientFile = AGE_RECIPIENT;

      var fstream = _setupInFile();
      var dest = ae.encrypt(fstream);

      fstream.Dispose();
      fstream = null;

      var test_out = _setupOutFile(AGE_DESTNAME);
      ae.ReceipientFile = AGE_KEY;
      ae.decrypt(dest, test_out);
      test_out.Dispose();
      test_out = null;
      dest = null;
      Assert.That(true);
    }


    private FileStream _setupInFile()
    {
      var fstream = new FileStream(testSRCPATH, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      return fstream;
    }

    private FileStream _setupOutFile(string destfile)
    {
      return new FileStream(Path.Combine(testDESTDIR, destfile), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
    }
  }
}

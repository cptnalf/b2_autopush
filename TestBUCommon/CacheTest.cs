using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TestBUCommon
{
  using FreezeFile = BUCommon.FreezeFile;
  using FileCache = BUCommon.FileCache;
//  using BackupLib;

  public class UnitTest1
  {
    private FileCache _buildCache()
    {
      var uc = new FileCache();

      var c = new BUCommon.Container { accountID=1, id="blargacct", name="blarg account", type="blarg" };
      var ff = new FreezeFile 
        { 
          fileID="blarg"
          , localHash=new BUCommon.Hash { type="SHA0", raw=new byte[] { 0, 22, 44, 11} } 
          , mimeType="application/byte-stream"
          , modified=new DateTime(2016,12,01)
          , path="blarg/blarg1.obj"
          , storedHash=BUCommon.Hash.Create("SHA0", new byte[] { 22,44, 0, 89 })
          , uploaded=new DateTime(2016,12,03)
          ,container = c
        };
      uc.add(c);
      uc.add(ff);

      return uc;
    }

    private string _fileSave()
    { return System.IO.Path.Combine(Environment.CurrentDirectory, "cachexml.xml"); }

    private void _basicChecks(FileCache uc)
    {
      var blargdir = uc.getdir("blarg");
      
      Assert.NotEmpty(blargdir);
      var item = blargdir.FirstOrDefault();
      Assert.NotNull(item);
      Assert.Equal("blarg/blarg1.obj", item.path);
    }

    [Fact]
    public void Basic()
    {
      var uc = _buildCache();

      _basicChecks(uc);

      var fs = uc.getContainer(1, "blargacct", null);
      Assert.NotNull(fs);
      Assert.NotEmpty(fs);

      var c = uc.containers.FirstOrDefault();
      Assert.NotNull(c);
    }

    [Fact]
    public void WriteXML()
    {
      var uc = _buildCache();
      uc.save(_fileSave());

      _basicChecks(uc);
    }

    [Fact]
    public void ReadXML()
    {
      var uc = _buildCache();
      
      uc.save(this._fileSave());

      uc = new FileCache();
      uc.load(this._fileSave());

      _basicChecks(uc);

      uc.load(this._fileSave());
      _basicChecks(uc);
    }
  }
}

/*
    [TestMethod]
    public void TestCacheWrite()
    {
      var uc = _buildCache();
      uc.save("c:\\tmp\\cachexml.xml");
    }

    [TestMethod]
    public void TestCacheRead()
    {
      var uc = _buildCache();
      uc.save("c:\\tmp\\cachexml.xml");

      uc.load("c:\\tmp\\cachexml.xml");
      var blardir = uc.getdir("blarg");

      Assert.IsNotNull(blardir);
      var item = blardir.FirstOrDefault();
      Assert.IsNotNull(item);
      Assert.AreEqual("blarg/blarg1.obj", item.path);
    }

    [TestMethod]
    public void TestAccountBuilderLoad()
    {
      var accts = BackupLib.AccountBuilder.BuildAccounts();
      Assert.IsNotNull(accts);
      Assert.IsNotNull(accts.filecache);
      Assert.IsTrue(accts.accounts.Count > 0);
      foreach(var acct in accts)
        {
          Assert.IsNotNull(acct.service);
          Assert.IsNotNull(acct.auth);
          Assert.IsNotNull(acct.service.fileCache);
          Assert.IsTrue(accts.filecache == acct.service.fileCache);
        }
    }

    [TestMethod]
    public void TestAccountBuilderSave()
    {
      var accts = BackupLib.AccountBuilder.BuildAccounts();

      var cont = new BUCommon.Container { accountID=2, id=@"C:\tmp\b2test\cont1", name="cont1" };
      accts.filecache.add(cont);
      var ff = new BUCommon.FreezeFile { container=cont, fileID=@"C:\tmp\b2test\cont1\2016\1231-newyears\DSC06560.ARW", path="2016/1231-newyears/DSC06560.ARW"};
      accts.filecache.add(ff);
      BackupLib.AccountBuilder.Save(accts);
    }
  }
}
*/
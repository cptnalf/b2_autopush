using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBackupLib
{
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;

  using NUnit.Framework;

  using FreezeFile = BUCommon.FreezeFile;
  using FileCache = BUCommon.FileCache;
  using BackupLib;

  [TestClass]
  public class CacheTest
  {
    private FileCache _buildCache()
    {
      var uc = FileCache.Load("./foo.cache");

      var c = new BUCommon.Container { accountID=1, id="blargacct", name="blarg account", type="blarg" };
      var ff = new FreezeFile 
        { 
          fileID="blarg"
          , localHash=BUCommon.Hash.FromString("SHA256", "9a35126376f514ca3eed8b7fa1e50041de59201d2c2d0dc21536110447fe3347") 
          , mimeType="application/byte-stream"
          , modified=new DateTime(2016,12,01)
          , path="blarg/blarg1.obj"
          , storedHash=BUCommon.Hash.FromString("SHA256", "9a35126376f514ca3eed8b7fa1e50041de59201d2c2d0dc21536110447fe3347")
          , uploaded=new DateTime(2016,12,03)
          ,container = c
        };
      uc.add(c);
      uc.add(ff);

      return uc;
    }
    
    [TestMethod]
    public void TestDelete()
    {
      var uc = _buildCache();

      var blargdir = uc.getdir("blarg");
      var x = blargdir.FirstOrDefault();
      var p = x.path;

      // this isn't supposed to be null.
      x.container = new BUCommon.Container { accountID=1, id="blargacct", name="blarg account", type="blarg" };
      uc.delete(x);

      blargdir = uc.getdir("blarg");
      Assert.IsFalse(blargdir.Any());
    }

    [TestMethod]
    public void TestCache()
    {
      var uc = _buildCache();

      var blargdir = uc.getdir("blarg");

      Assert.IsTrue(blargdir.Any());
      var item = blargdir.FirstOrDefault();
      Assert.IsNotNull(item);
      Assert.AreEqual("blarg/blarg1.obj", item.path);
    }

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
      uc.save("c:\\tmp\\cache.json");

      uc = FileCache.Load("c:\\tmp\\cache.json");
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
      Assert.That(accts, Is.Not.Null);
      Assert.That(accts.filecache, Is.Not.Null);
      Assert.That(accts.accounts, Is.Not.Empty);
      foreach(var acct in accts.accounts)
        {
          Assert.That(acct.service, Is.Not.Null);
          Assert.That(acct.auth, Is.Not.Null);
          Assert.That(acct.service.fileCache, Is.Not.Null);
          Assert.That(accts.filecache, Is.EqualTo(acct.service.fileCache));
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

    private void _testCurCache(string name, BUCommon.FileCache cache
      ,IReadOnlyList<BUCommon.Container> conts
      , Func<BUCommon.FileCache,BUCommon.Container, IQueryable<FreezeFile>> testMeth)
    {
      var timepercont = new Dictionary<string, TimeSpan>();
      var sw = new System.Diagnostics.Stopwatch();

      int x = 0;
      do {
      foreach(var c in conts)
        {
          //var files = cache.getContainer(c.accountID, c.id, null);
          var files = testMeth(cache, c);
          sw.Start();
          var q = files.ToList();
          sw.Stop();

          if (!timepercont.ContainsKey(c.id)) { timepercont.Add(c.id, new TimeSpan()); }
          timepercont[c.id] += sw.Elapsed;
          
          //ms += sw.Elapsed;
          //cnt += q.Count;
        }
        ++x;
      }while(x < 5);

      //TestContext.Progress.WriteLine("{0} files: {1}", c.name, cnt);
      foreach(var k in timepercont.Keys)    
        {
          var ms = timepercont[k];
          TestContext.Progress.WriteLine("{3} filelist: {0:00}m {1:00}.{2:000}s"
            , ms.Minutes, ms.Seconds, ms.Milliseconds, name);
        }
    }

    [Test]
    public void TestCurCacheLoad()
    {
      var cache = BUCommon.FileCache.Load("/home/chiefengineer/.b2app/b2app.filecache.json");

      var sw = new System.Diagnostics.Stopwatch();
      sw.Start();
      var conts = cache.getContainers(4);
      sw.Stop();
      TestContext.Progress.WriteLine("contload: {0:00}:{1:00}:{2:00}.{3:00}",
            sw.Elapsed.Hours, sw.Elapsed.Minutes, sw.Elapsed.Seconds,
            sw.Elapsed.Milliseconds / 10);
      TestContext.Progress.WriteLine("containers: {0}", conts.Count);
      long cnt = 0;
      //TimeSpan ms = new TimeSpan();
      _testCurCache("getContainer", cache, conts, (fc, c) => 
        {
          return fc.getContainer(c.accountID, c.id, null);
        });

      _testCurCache("getContNoHash", cache, conts, (fc, c) =>
        {
          return fc.getContNoHash(c.accountID, c.id, null);
        });
    }

    [Test]
    public void TestCurLocFile()
    {
      var ll = new BackupLib.LocalLister();
      var sw = new System.Diagnostics.Stopwatch();
      
      sw.Start();
      var files = ll.getList("/data2/photos", "(^export/|node_modules/)", null);
      sw.Stop();

      TestContext.Progress.WriteLine("contload: {0:00}:{1:00}:{2:00}.{3:00}  => {4}",
            sw.Elapsed.Hours, sw.Elapsed.Minutes, sw.Elapsed.Seconds,
            sw.Elapsed.Milliseconds / 10
            ,files.Count);

    }
  }
}

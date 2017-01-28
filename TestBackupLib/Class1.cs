using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestBackupLib
{
  using Microsoft.VisualStudio.TestTools.UnitTesting;

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
  }
}

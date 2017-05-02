using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestBackupLib
{
  [TestClass]
  public class CommandTests
  {
    protected BUCommon.AccountList _makeSvc()
    {
      var acct = new BUCommon.Account();
      acct.connStr = @"c:\tmp\b2test";
      acct.svcName = "BackupLib.LocalService";
      acct.id=1;
      acct.name = "localtest";
      var acctlst = new BUCommon.AccountList();
      acctlst.filecache = new BUCommon.FileCache();

      System.IO.Directory.CreateDirectory(@"c:\tmp\b2test\cont1");

      if (System.IO.File.Exists(@"c:\tmp\b2test\cache_sync.xml"))
        { acctlst.filecache.load(@"c:\tmp\b2test\cache_sync.xml"); }
      
      BackupLib.AccountBuilder.Load(acctlst, acct);
      acctlst.Add(acct);

      return acctlst;
    }

    [TestMethod]
    public void AuthorizeTest()
    {
      var accts = _makeSvc();
      var acct = accts.accounts[0];

      var auth = new BackupLib.commands.Authorize { account=acct };
      auth.run();
    }

    [TestMethod]
    public void ContainersTest()
    {
      var accts = _makeSvc();
      var acct = accts.accounts[0];

      var conts = new BackupLib.commands.Containers { account=acct, cache=accts.filecache};
      conts.run();

      Assert.IsNotNull(accts.filecache.containers);
      Assert.IsTrue(accts.filecache.containers.Count > 0);
    }

    [TestMethod]
    public void FileList()
    {
      var accts = _makeSvc();
      var acct = accts.accounts[0];
      bool haveCache = false;

      if (System.IO.File.Exists(@"c:\tmp\b2test\cache_sync.xml"))
        { 
          haveCache = true;
          accts.filecache.load(@"c:\tmp\b2test\cache_sync.xml"); 
        }

      var conts = new BackupLib.commands.Containers { account=acct, cache=accts.filecache};
      conts.run();

      var cont = accts.filecache.containers[0];

      var cr = new BackupLib.commands.FileList 
        { account=acct, cache=accts.filecache, container=cont, useRemote=!haveCache };
      var res = cr.run();
      Assert.IsNotNull(res);
      Assert.IsTrue(res.Count > 0, "Maybe run a sync first?");

      accts.filecache.save(@"c:\tmp\b2test\cache_sync.xml");
    }


    [TestMethod]
    public void CopyRemote()
    {
      var accts = _makeSvc();
      var acct = accts.accounts[0];

      var conts = new BackupLib.commands.Containers { account=acct, cache=accts.filecache};
      conts.run();

      var cont = accts.filecache.containers[0];

      var cr = new BackupLib.commands.CopyRemote 
        { 
          account=acct
          , cache=accts.filecache
          , container=cont
          , pathRoot=@"c:\tmp\photos"
          , key=@"c:\tmp\id_rsa_1_pub"
        };
      cr.run();
    }

    [TestMethod]
    public void CopyLocal()
    {
      var accts = _makeSvc();
      var acct = accts.accounts[0];

      var conts = new BackupLib.commands.Containers { account=acct, cache=accts.filecache};
      conts.run();

      BUCommon.FreezeFile rmt = null;

      var cont = accts.filecache.containers[0];
      if (cont.files.Count > 0)
        {
          rmt = cont.files[0];
        }

      Assert.IsNotNull(rmt);
      Assert.IsNotNull(rmt.fileID);

      var cl = new BackupLib.commands.CopyLocal
        {
          account=acct
          , cont=cont
          , noAction=true
          , filterre=rmt.path
          , key= @"c:\tmp\id_rsa_1"
          , destPath=@"c:\tmp\photos1"
        };

      System.IO.Directory.CreateDirectory(cl.destPath);
      cl.run();
    }

    [TestMethod]
    public void Sync()
    {
      var accts = _makeSvc();
      var acct = accts.accounts[0];

      var conts = new BackupLib.commands.Containers { account=acct, cache=accts.filecache};
      conts.run();

      var cont = accts.filecache.containers[0];

      var cr = new BackupLib.commands.Sync 
          { account=acct, cache=accts.filecache, container=cont
            , keyFile=@"C:\tmp\id_rsa_1_pub", pathRoot=@"C:\tmp\photos"};
      cr.run();

      accts.filecache.save(@"c:\tmp\b2test\cache_sync.xml");
    }
  }
}

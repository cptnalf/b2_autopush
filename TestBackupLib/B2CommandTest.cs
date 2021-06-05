using System;
using System.Collections.Generic;
using System.Linq;

using TestMethod = NUnit.Framework.TestAttribute;

namespace TestBackupLib
{
  using NUnit.Framework;
  
  [TestFixture]
  public class B2CommandTest
  {
    private BUCommon.AccountList _accts = null;
    protected string acctname => "b2";
    protected string testRoot => @"c:\tmp\photos";
    protected string testDest => @"c:\tmp\photos1";
    protected string pubKey => @"c:\tmp\id_rsa_1_pub";
    protected string privateKey => @"c:\tmp\id_rsa_1";

    [TestMethod]
    public void CmdAuthorize()
    {
      var acct = _getAcct();
      var auth = new BackupLib.commands.Authorize {account=acct, accounts=_accts};
      auth.run();
    }

    [TestMethod]
    public void CmdContainerList()
    {
      var acct = _getAcct();
      var conts = new BackupLib.commands.Containers { account=acct, cache=acct.service.fileCache};
      conts.run();
      Assert.IsNotNull(acct.service.fileCache.containers);
      Assert.IsTrue(acct.service.fileCache.containers.Where(x => x.accountID == acct.id).Any());
    }

    private void _CmdFileList(bool forceRemote)
    {
      var acct = _getAcct();

      var cont = acct.service.fileCache.containers.Where(x => x.accountID== acct.id).FirstOrDefault();
      if (cont == null) { Assert.Fail("no containers in cache!"); }
      
      var file = acct.service.fileCache.getdir(cont.name)
        .FirstOrDefault();

      bool rmt = true;
      if (!forceRemote && file != null) { rmt = false; }

      var filelist = new BackupLib.commands.FileList { account=acct, cache=acct.service.fileCache, container=cont, useRemote=rmt};
      var res = filelist.run();
      Assert.That(res, Is.Not.Null);
      Assert.That(res, Is.Not.Empty, "No files were found! (check remote for files)");
    }

    [TestMethod]
    public void CmdFileList()
    {
      _CmdFileList(false);
    }

    [TestMethod]
    public void CmdFileListRemote()
    {
      _CmdFileList(true);
    }

    [TestMethod]
    public void CmdCopyRemote()
    {
      var acct = _getAcct();

      var cont = acct.service.fileCache.getContainers(acct.id).FirstOrDefault();

      var cr = new BackupLib.commands.CopyRemote 
        { 
          account=acct
          , cache=acct.service.fileCache
          , container=cont
          , pathRoot=this.testRoot
          , key=this.pubKey
        };
      cr.run();
    }

    [TestMethod]
    public void CmdSync()
    {
      var acct = _getAcct();
      
      var cont = acct.service.fileCache.getContainers(acct.id).FirstOrDefault();

      var cr = new BackupLib.commands.Sync 
          { 
            account=acct
            , cache=acct.service.fileCache
            , container=cont
            , keyFile=pubKey
            , pathRoot=testRoot
          };
      cr.run();
    }

    [Test]
    public void CmdCopyLocal()
    {
      var acct = _getAcct();

      var cont = acct.service.fileCache.getContainers(acct.id).FirstOrDefault();

      BUCommon.FreezeFile rmt = null;
      if (cont.files.Count > 0) { rmt = cont.files[0]; }

      Assert.IsNotNull(rmt);
      Assert.IsNotNull(rmt.fileID);

      var cl = new BackupLib.commands.CopyLocal
        {
          account=acct
          , key= privateKey
          , destPath=testDest
          , cont=cont
          , maxTasks=1
          , noAction= true
          , filterre=rmt.path
        };

      System.IO.Directory.CreateDirectory(cl.destPath);
      cl.run();
    }

    [TearDown]
    public void _cleanup()
    {
      if (_accts != null)
        { BackupLib.AccountBuilder.Save(_accts); }
    }

    protected void _loadAccounts() { _accts = BackupLib.AccountBuilder.BuildAccounts(); }
    protected BUCommon.Account _getAcct()
    { 
      if (_accts == null) { _loadAccounts(); }
      return _accts.accounts.Where(x => x.name == acctname).FirstOrDefault(); 
    }
  }
}

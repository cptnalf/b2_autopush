﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace b2app
{
  class Program
  {
    static System.Text.RegularExpressions.Regex _CmdRE = new System.Text.RegularExpressions.Regex("^BackupLib.commands", System.Text.RegularExpressions.RegexOptions.Compiled);
    
    static void Main(string[] args)
    {
      CommandLine.Parser.Default.ParseArguments<Options.AccountsOpt,Options.AuthOpt,Options.ContOpt,Options.LSOpt,Options.SyncOpts,Options.CopyOpts>(args)
        .MapResult(
          (Options.AccountsOpt o) => Actions.Accounts(o)
          ,(Options.AuthOpt o1) => Actions.Auth(o1)
          ,(Options.ContOpt o4) => Actions.ListContainers(o4)
          ,(Options.LSOpt o2) => Actions.ListFiles(o2)
          , (Options.SyncOpts o3) => Actions.Sync(o3)
          , (Options.CopyOpts o5) => Actions.Copy(o5)
          ,(IEnumerable<Error> errs) => { foreach(var e in errs) { Console.WriteLine(e.Tag); } return 0; }
          );
      /*
      Console.WriteLine("options:");
      
      var asm = System.Reflection.Assembly.GetAssembly(typeof(BackupLib.AccountBuilder));
      foreach(var t in asm.ExportedTypes.Where(x => x.GetInterfaces().Where(t => t == typeof(BUCommon.ICommand)).Any())
                        .OrderBy(x => x.FullName))
        {
          var tc = t.GetConstructor(new Type[] {});
          var o = tc.Invoke(new object[] {});
          var cmd = o as BUCommon.ICommand;
          Console.WriteLine("\t{0}", cmd.helptext);
        }
      */

      /*
       * load defaults:
       * all options would be parsed, just maybe not used.
       * keys should be defaulted to account and cache storage locations.
       * 
       * need a 'default account' creation tool that would generate
       * a local-service account pointing to a directory in their home.
       * - easy to test usage, and maybe provide a why to construct accounts manually.
       *   atleast until an account-creation tool is constructed.
       * 
       */

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

      BUCommon.AccountList accts = new BUCommon.AccountList();
      accts.load("accounts.xml");
      var acct = accts.accounts.FirstOrDefault();

      if (acct == null)
        {
          var contents = string.Empty;
          {
            var keyf = new System.IO.FileStream("b2.key", System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            var strm = new System.IO.StreamReader(keyf);
            while(!strm.EndOfStream) 
              { var str = strm.ReadLine(); if (!string.IsNullOrWhiteSpace(str) && str[0] != '#') { contents = str; } }

            strm.Close();
            keyf = null;
            contents = contents.Trim();
          }

          acct = accts.create("b2");
          acct.connStr = contents;
          acct.svcName = "CommB2.Connection";
          BackupLib.AccountBuilder.Load(acct);
        }

      accts.save("accounts.xml");
      
    }
  }
}

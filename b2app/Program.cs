using System;
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
    static int Run(BackupCmd a)
    {
      int res = 1;
      var accts = BackupLib.AccountBuilder.BuildAccounts();

      try { res = a.run(accts); }
      catch(Exception e)
        {
          var x = e;
          while(x != null)
            {
              _print(x);
              x = x.InnerException;
            }

          res = 1;
        }
      finally
        {
          BackupLib.AccountBuilder.Save(accts);
        }

      return res;
    }

    static void _print(Exception e)
    {
      Console.WriteLine(e.GetType().Name);
      Console.WriteLine(e.Message);
      Console.WriteLine(e.Source);
      if (e.TargetSite != null) { Console.WriteLine(e.TargetSite.Name); }
      Console.WriteLine(e.StackTrace);
      Console.WriteLine("----------------------------");
      if (e.InnerException != null) { Console.WriteLine("inner exception:"); }
    }

    static void Main(string[] args)
    {
      var types = new Type[]
        { typeof(Options.AccountsOpt), typeof(Options.AuthOpt)
          ,typeof(Options.ContOpt), typeof(Options.LSOpt)
          ,typeof(Options.SyncOpts), typeof(Options.CopyOpts)
          ,typeof(Options.AddAccountOptions), typeof(Options.ConfigOpts)
          ,typeof(Options.EditAccountOpts)
          ,typeof(Options.EditContainer)
        };

      var res = CommandLine.Parser.Default.ParseArguments(args, types);
      
      res
        .WithParsed<Options.AccountsOpt>( (o) => Run(new Accounts(o)))
        .WithParsed<Options.AuthOpt>(( o1) => Run(new Auth(o1)))
        .WithParsed<Options.ContOpt>(( o4) => Run(new ListContainers(o4)))
.WithParsed<Options.LSOpt>(( o2) => Run(new ListFiles(o2)))
          .WithParsed<Options.SyncOpts>(( o3) => Run(new Sync(o3)))
          .WithParsed<Options.CopyOpts>(( o5) => Run(new Copy(o5)))
          .WithParsed<Options.AddAccountOptions>(( o6) => Run(new AddAccount(o6)))
          .WithParsed<Options.ConfigOpts>(( o7) => Run(new ConfigCmd(o7)))
          .WithParsed<Options.EditContainer>((o8) => Run(new EditContainer(o8)))
          ;

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
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;

namespace b2app.Options
{
 public class CopyLoc
  {
    public BUCommon.Container cont {get;set;}
    public BUCommon.Account acct {get;set;}
  }

  [Verb("copy", HelpText = "copy files from one destintation to another, can be local/remote. src/dest are of the format <acct>:<container>")]
  public class CopyOpts
  {
    [Value(0, Required=true,HelpText ="source for files.")]
    public string source {get;set;}

    [Value(1, Required =true, HelpText ="destination for files")]
    public string dest {get;set;}

    [Option('k', Required =true, HelpText ="key file (copy remote=pub key, copy local=private key)")]
    public string key {get;set;}

    [Option('f', HelpText ="Filter files to copy (.net Regular Expression)")]
    public string filterRE {get;set;}

    [Option('n', Default =false, HelpText ="lists what files would be copied to the destination.")]
    public bool dryrun {get;set;}

    public static CopyLoc ParseLoc(BUCommon.AccountList accts, string loc)
    {
      CopyLoc l = new CopyLoc();

      if (System.IO.Directory.Exists(loc)) { l.cont = new BUCommon.Container { accountID=0, id=loc, name=loc, type="LOCAL"}; }
      else
        {
          var parts = loc.Split(':');
          if (parts != null && parts.Length > 1)
            {
              if (parts[0].Length == 1) 
                { 
                  return l;
                }

              var acct = accts.accounts.Where(x => x.name == parts[0]).FirstOrDefault();
              if (acct != null)
                {
                  var cont = accts.filecache.containers.Where(x => x.accountID == acct.id && x.name== parts[1]).FirstOrDefault();

                  if (cont != null)
                    { l.acct=acct; l.cont=cont; }
                }
            }
        }

      return l;
    }
  }
}
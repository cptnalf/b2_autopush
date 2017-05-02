using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace b2app.Options
{
  using CommandLine;

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
  }
}

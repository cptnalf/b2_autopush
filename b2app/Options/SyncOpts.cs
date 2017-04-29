using System;
using CommandLine;

namespace b2app.Options
{
  [Verb("sync", HelpText = "sync local files to remote, using cache to compare uploaded files (use ls to refresh cache)")]
  public class SyncOpts
  {
    [CommandLine.Option('a', Required =true, HelpText ="account to sync with")]
    public string account{get;set;}

    [CommandLine.Option('c', Required =true,HelpText ="container to place the files in")]
    public string container{get;set;}
    [CommandLine.Option('k', Required =true, HelpText ="path to keyfile (pushing to remote, need pub key)")]
    public string keyfile {get;set;}

    [CommandLine.Option('p', Required=true, HelpText ="root path to sync")]
    public string pathroot {get;set;}

    [Option('r', Default=false, HelpText ="query remote host only (skip cache)")]
    public bool useremote {get;set;}

    [Option('n', Default=false, HelpText ="lists what would happen instead of doing things")]
    public bool dryrun {get;set;}
  }
}
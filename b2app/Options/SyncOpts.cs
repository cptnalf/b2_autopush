using System;

namespace b2app.Options
{
  using CommandLine;

  [Verb("sync", HelpText = "sync local files to remote, using cache to compare uploaded files (use ls to refresh cache)")]
  public class SyncOpts
  {
    [Option('a', Required =true, HelpText ="account to sync with")]
    public string account{get;set;}

    [Option('c', Required =true,HelpText ="container to place the files in")]
    public string container{get;set;}
    [Option('k', Required =true, HelpText ="path to keyfile (pushing to remote, need pub key)")]
    public string keyfile {get;set;}
    
    [Option(Default=null, HelpText ="private key for decrypting remote localhash")]
    public string privateKey {get;set;}

    [Option('p', Required=true, HelpText ="root path to sync")]
    public string pathroot {get;set;}

    [Option('r', Default=false, HelpText ="query remote host only (skip cache)")]
    public bool useremote {get;set;}

    [Option('n', Default=false, HelpText ="lists what would happen instead of doing things")]
    public bool dryrun {get;set;}

    [Option('j', Default =5, HelpText ="degree of parallelism for uploads")]
    public int maxTasks {get;set;}

    [Option('s', Default =false, HelpText ="compare by hash instead of by upload/modified date")]
    public bool cksum {get;set;}
    
    [Option('f', HelpText ="filter to sync only these files.")]
    public string filterre {get;set;}

    [Option('e', HelpText="exclude these files")]
    public string excludere {get;set;}
  }
}
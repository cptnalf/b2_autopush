using System;

namespace b2app.Options
{
  using CommandLine;

  [Verb("ls", HelpText ="list files in a container")]
  public class LSOpt
  {
    [Option('a', "account", Required=true,HelpText ="account to use")]
    public string account {get;set;}

    [Option('c', "container", Required =true, HelpText ="Container to list")]
    public string container {get;set;}

    [Option('r', "remote", Default=false, HelpText ="query remote host only (skip cache)")]
    public bool useremote {get;set;}

    [Option('f', "filter", HelpText ="Regex filter for files")]
    public string filter {get;set;}

    [Option('v', "versions", Default=false, HelpText ="Get versions too")]
    public bool versions {get;set;}
  }
}
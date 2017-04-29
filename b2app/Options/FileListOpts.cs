using System;
using CommandLine;

namespace b2app.Options
{
  [Verb("ls", HelpText ="list files in a container")]
  public class LSOpt
  {
    [Option('a', Required=true,HelpText ="account to use")]
    public string account {get;set;}

    [Option(Required =true, HelpText ="Container to list")]
    public string container {get;set;}

    [Option('r', Default=false, HelpText ="query remote host only (skip cache)")]
    public bool useremote {get;set;}

    [Option('f', HelpText ="Regex filter for files")]
    public string filter {get;set;}

    [Option('v', Default=false, HelpText ="Get versions too")]
    public bool versions {get;set;}
  }
}
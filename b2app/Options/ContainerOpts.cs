using System;

namespace b2app.Options
{
  using CommandLine;
  
  [Verb("containers", HelpText ="List containers in an account")]
  public class ContOpt
  {
    [Option('a', Required=true, HelpText ="account name to use")]
    public string account {get;set;}
  }
}

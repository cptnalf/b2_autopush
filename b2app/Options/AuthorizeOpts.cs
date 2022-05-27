using System;

namespace b2app.Options
{
  using CommandLine;

  [Verb("authorize", HelpText ="Authorize an account")]
  public class AuthOpt
  {
    [Option('a', "account", Required=true, HelpText = "account name to use" )]
    public string account {get;set;}
  }
}

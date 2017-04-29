using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace b2app.Options
{
  [CommandLine.Verb("authorize", HelpText ="Authorize an account")]
  public class AuthOpt
  {
    [CommandLine.Option('a', Required=true, HelpText = "account name to use" )]
    public string account {get;set;}
  }
}
using System;

namespace b2app.Options
{
  using CommandLine;

  [Verb("editAccount", HelpText = "edit account settings.")]
  public class EditAccountOpts
  {
    [Option('a',"use-age", Default=false, HelpText ="encrypt with age. this uses the age program to encrpyt instead of the built-in method.")]
    public bool useAge {get;set;}

    [Option('b', "built-in", Default=true, HelpText ="encrypt with the built-in method.")]
    public bool useBuiltin { get;set;}
  }
}
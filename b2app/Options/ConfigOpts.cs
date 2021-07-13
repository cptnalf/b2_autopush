namespace b2app.Options
{
  using CommandLine;

  [Verb("config", HelpText = "set some app configuration settings")]
  public class ConfigOpts
  {
    [Option('a', "age-bin", HelpText ="set the age binary path. (defaults to in the path)")]
    public string ageBin {get;set;}
  }
}

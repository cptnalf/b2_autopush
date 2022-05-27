namespace b2app.Options
{
  using CommandLine;

  [Verb("config", HelpText = "set some app configuration settings")]
  public class ConfigOpts
  {
    [Option('s', "set", HelpText="Change settings (defaults to just displaying settins)")]
    public bool change {get;set;}
    [Option('a', "age-bin", HelpText ="set the age binary path. (defaults to in the path)")]
    public string ageBin {get;set;}
  }
}

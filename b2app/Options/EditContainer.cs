namespace b2app.Options
{
  using CommandLine;

  [Verb("editcont", HelpText="Edit container options (change encrpytion)" )]
  public class EditContainer
  {
    [Option('a',  Required =true, HelpText ="account name to use" )]
    public string account {get;set;}

    [Option('c', Required =true, HelpText ="container to change")]
    public string container {get;set;}

    [Option('e', HelpText ="use age encrpytion? (set means yes)")]
    public bool useAge {get;set;} 
  } 
}

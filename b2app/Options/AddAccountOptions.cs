namespace b2app.Options
{
  using CommandLine;
  [Verb("addAccount", HelpText="Add an Account")]
  public class AddAccountOptions
  {
    [Option('n', "name", Required=true, HelpText="name of the account, short, no spaces")]
    public string name {get;set;}

    [Option('t', "type", Required=true, HelpText="Type of the account: local, b2")]
    public string type {get;set;}

    [Option('u', "user", Required=false, HelpText="User or account id/number/name (B2=acccount id)")]
    public string user {get;set;}

    [Option('p', "password", Required=false, HelpText="Password or token (B2=token)")]
    public string password {get;set;}

    [Option('d', "directory", Required=false, HelpText="base directory for local accounts (encrypted files are placed here)")]
    public string directory {get;set;}
  }
}
namespace b2app
{
  internal class ConfigCmd : ProgBackupCmd
  {
    private Options.ConfigOpts _opts;
    public ConfigCmd(Options.ConfigOpts o) { _opts = o; }

    public override int run(BUCommon.AccountList accounts)
    {
      if (accounts.AgePath != _opts.ageBin)
        {
          System.Console.WriteLine("Change age path from {0} to {1}", accounts.AgePath, _opts.ageBin);
          accounts.AgePath = _opts.ageBin;
        }
      
      return 0;
    }
  }
}
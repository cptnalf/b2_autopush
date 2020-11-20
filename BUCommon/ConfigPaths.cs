using System;
using System.IO;

namespace BUCommon
{
  public static class ConfigPaths
  {
    private static Lazy<string> _Prefix = new Lazy<string>(() => {
      string prefix = string.Empty;

      {
        var os = Environment.OSVersion;
        switch (os.Platform)
          {
            case (PlatformID.Unix):
              {
                prefix = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                prefix = Path.Combine(prefix, ".b2app");
                /* this should yield something like: /home/foo */
                break;
              }
            case (PlatformID.Win32NT):
              {
                /* this should yield something like: c:\users\user1\documents */
                prefix = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
                break;
              }
          }
      }
      
      return prefix;
    });

    public static string Prefix { get { return _Prefix.Value; } }
  }
}
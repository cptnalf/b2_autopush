using System;
using System.Collections.Generic;

namespace BackupLib
{
  public static class OSFileEqualityComparer
  {
    public static IEqualityComparer<string> Comparer()
    {
      switch (Environment.OSVersion.Platform)
        {
          case (PlatformID.Unix): { return StringComparer.CurrentCulture; }
        }
      return StringComparer.CurrentCultureIgnoreCase;
    } 
  }
}

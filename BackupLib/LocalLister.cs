using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackupLib
{
  using System.IO;
  using BUCommon;

  public class LocalLister : IFileLister
  {
    public IReadOnlyList<FreezeFile> getList(string root)
    {
      List<FreezeFile> lst = new List<FreezeFile>();

      _enum(root, string.Empty, ref lst);

      return lst;
    }

    /// <summary>
    /// return a list of all files below a certian path.
    /// the returned files will have relative paths.
    /// </summary>
    /// <param name="dir">starting directory</param>
    /// <param name="root">starting path part (can be empty or null)</param>
    /// <param name="files">the resulting list of files</param>
    private void _enum(string dir, string root, ref List<FreezeFile> files)
    {
      const string pastePath = "{0}/{1}";
      int lenp = 0;
      
      if (string.IsNullOrWhiteSpace(dir)) { return; }
      if (!Directory.Exists(dir)) { return; }
      if (dir[dir.Length-1] == System.IO.Path.DirectorySeparatorChar
	  || dir[dir.Length -1] == System.IO.Path.AltDirectorySeparatorChar)
        { lenp = 0; }
      else { lenp += 1; }

      var dfiles = Directory.EnumerateFiles(dir);
      foreach(var f in dfiles)
        {
          /* hash them? */
          string newpath = f.Substring(dir.Length + lenp);
          if (!string.IsNullOrWhiteSpace(root)) { newpath = string.Format(pastePath, root, newpath); }

          DateTime updated = File.GetLastWriteTimeUtc(f);

          files.Add(new FreezeFile { path = newpath, modified=updated });
        }

      var dirs = Directory.EnumerateDirectories(dir);
      foreach(var d in dirs) 
        {
          string newroot = d.Substring(dir.Length + lenp);
          if (!string.IsNullOrWhiteSpace(root)) { newroot = string.Format(pastePath, root, newroot); }
          _enum(d, newroot, ref files);
        }
    }
  }
}

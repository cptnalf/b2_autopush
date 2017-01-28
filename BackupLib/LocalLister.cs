using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackupLib
{
  using System.IO;

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
      if (string.IsNullOrWhiteSpace(dir)) { return; }
      if (!Directory.Exists(dir)) { return; }

      var dfiles = Directory.EnumerateFiles(dir);
      foreach(var f in dfiles)
        {
          /* hash them? */
          string newpath = f.Substring(dir.Length + 1);
          if (!string.IsNullOrWhiteSpace(root)) { newpath = root + "/" + newpath; }

          DateTime updated = File.GetLastWriteTimeUtc(f);

          files.Add(new FreezeFile { path = newpath, uploaded=updated });
        }

      var dirs = Directory.EnumerateDirectories(dir);
      foreach(var d in dirs) 
        {
          string newroot = d.Substring(dir.Length + 1);
          if (!string.IsNullOrWhiteSpace(root)) { newroot = root + "/" + newroot; }
          _enum(d, newroot, ref files);
        }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackupLib
{
  using System.IO;
  using System.Text.RegularExpressions;
  using BUCommon;

  public class LocalLister : IFileLister
  {
    /** exclusion regex. */
    private Regex _reExc;
    /** inclusive regex */
    private Regex _reInc;

    public IReadOnlyList<FreezeFile> getList(string root, string excludeRe, string includeRe)
    {
      List<FreezeFile> lst = new List<FreezeFile>();

      _reExc = null;
      _reInc = null;

      if (!string.IsNullOrWhiteSpace(excludeRe))
        {
          _reExc = new Regex(excludeRe, RegexOptions.Compiled| RegexOptions.IgnoreCase);
        }
      
      if (!string.IsNullOrWhiteSpace(includeRe))
        {
          _reInc = new Regex(includeRe, RegexOptions.Compiled| RegexOptions.IgnoreCase); 
        }


      _enum(root, string.Empty, ref lst, excludeRe, includeRe);

      return lst;
    }

    /// <summary>
    /// return a list of all files below a certian path.
    /// the returned files will have relative paths.
    /// </summary>
    /// <param name="dir">starting directory</param>
    /// <param name="root">starting path part (can be empty or null)</param>
    /// <param name="files">the resulting list of files</param>
    /// <remarks>
    /// the returned FreezeFile here does not have a container.
    /// these just include path and modified date information.
    /// </remarks>
    private void _enum(string dir, string root, ref List<FreezeFile> files, string excludeRE, string incRE)
    {
      const string pastePath = "{0}/{1}";
      int lenp = 0;
      
      if (string.IsNullOrWhiteSpace(dir)) { return; }
      if (!Directory.Exists(dir)) { return; }
      if (   dir[dir.Length-1] == System.IO.Path.DirectorySeparatorChar
	        || dir[dir.Length -1] == System.IO.Path.AltDirectorySeparatorChar)
        { lenp = 0; }
      else { lenp += 1; }

      /* apply filters. */
      if (_reExc != null && _reExc.IsMatch(pastePath)) { return; }
      if (_reInc != null && !_reInc.IsMatch(pastePath)) { return; }

      /* pull files. */
      var dfiles = Directory.EnumerateFiles(dir);
      foreach(var f in dfiles)
        {
          /* hash them? */
          string newpath = f.Substring(dir.Length + lenp);
          if (!string.IsNullOrWhiteSpace(root)) { newpath = string.Format(pastePath, root, newpath); }

          DateTime updated = File.GetLastWriteTimeUtc(f);

          if (_reInc != null && !_reInc.IsMatch(root + newpath)) { continue; }
          if (_reExc != null && _reExc.IsMatch(root + newpath)) { continue; }
          
          files.Add(new FreezeFile { path = newpath, modified=updated });
        }

      /* process directories now. */
      var dirs = Directory.EnumerateDirectories(dir);
      foreach(var d in dirs) 
        {
          string newroot = d.Substring(dir.Length + lenp);
          if (!string.IsNullOrWhiteSpace(root)) { newroot = string.Format(pastePath, root, newroot); }
          _enum(d, newroot, ref files, excludeRE, incRE);
        }
    }
  }
}

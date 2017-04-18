using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib.commands
{
  /// <summary>
  /// ignore cache and push contents of directory to remote container.
  /// </summary>
  public class CopyRemote
  {
    public BUCommon.FileCache cache {get;set;}
    public BUCommon.Account account {get;set;}
    public BUCommon.Container container {get;set;}
    public string pathRoot {get;set;}
    public string fileRE {get;set;}

    public void run()
    {
      var ll = new LocalLister();
      var localfiles = ll.getList(pathRoot);

      if (!string.IsNullOrWhiteSpace(fileRE))
        {
          var re = new System.Text.RegularExpressions.Regex(fileRE, System.Text.RegularExpressions.RegexOptions.Compiled);

          localfiles = localfiles.Where(x => re.IsMatch(x.path)).ToList();
        }
      /* need multithreaded uploader. */
    }
  }
}

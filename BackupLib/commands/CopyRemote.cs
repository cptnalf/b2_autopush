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
  public class CopyRemote : BUCommon.ICommand
  {
    public string helptext => @"copyremote <account>:<container> <root> [file regex]
        -  Copies files from the local system, starting at <root> to the
         remote container.  It uses the optional [file regex] regular 
         expression to filter the list of local files to upload. The files
         in question are unconditionally uploaded, no checks are made to 
         see if any have already been uploaded. All the files are uploaded
         to the remote container.
";
    
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

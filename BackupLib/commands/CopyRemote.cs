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
    public string key {get;set;}

    public Action<FileDiff> progress {get;set;}
    public bool noAction { get;set; }

    public void run()
    {
      var ll = new LocalLister();
      var localfiles = ll.getList(pathRoot, null, fileRE);

      if (!string.IsNullOrWhiteSpace(fileRE))
        {
          var re = new System.Text.RegularExpressions.Regex(fileRE
            , System.Text.RegularExpressions.RegexOptions.Compiled| System.Text.RegularExpressions.RegexOptions.IgnoreCase);

          localfiles = localfiles.Where(x => re.IsMatch(x.path)).ToList();
        }

      var locdiffs = localfiles.Select(x => new FileDiff { local=x,remote=null,type= DiffType.created});
      var dp = new DiffProcessor { account=account, container=container, maxTasks=10, root=pathRoot, encKey=key};

      dp.noAction = noAction;
      dp.progressHandler = progress;
      dp.runType = RunType.upload;

      dp.add(locdiffs);
      dp.run();
    }
  }
}

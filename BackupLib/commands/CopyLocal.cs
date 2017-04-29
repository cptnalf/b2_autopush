using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib.commands
{
  using BUCommon;

  /// <summary>
  /// so, this probably should never exist.
  /// </summary>
  public class CopyLocal
  {
    public FreezeFile file {get;set;}
    public Account account {get;set;}
    public string destPath {get;set;}
    public string key {get;set;}
    public bool noAction {get;set;}
    public Action<FileDiff> progress {get;set;}
    
    public void run()
    {
      var dp = new DiffProcessor { account=account, container=null, encKey=key, maxTasks=10, root=destPath};

      dp.noAction = noAction;
      dp.runType = RunType.download;
      dp.progressHandler = progress;

      dp.add(new FileDiff{local=null, remote=file, type= DiffType.created});
      dp.run();
    }
  }
}

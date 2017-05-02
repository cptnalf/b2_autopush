using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib.commands
{
  using BUCommon;
  using System.Text.RegularExpressions;

  /// <summary>
  /// so, this probably should never exist.
  /// </summary>
  public class CopyLocal
  {
    public int maxTasks {get;set;}
    public Container cont {get;set;}
    public Account account {get;set;}
    public string destPath {get;set;}
    public string key {get;set;}
    public bool noAction {get;set;}
    public string filterre {get;set;}
    public Action<FileDiff> progress {get;set;}
    public Action<FileDiff,Exception> errors {get;set;}
    
    public void run()
    {
      var dp = new DiffProcessor 
        { 
          account=account
          , container=null
          , encKey=key
          , maxTasks=maxTasks
          , root=destPath
          , noAction=noAction
          , progressHandler=progress
          , errorHandler=errors
          , runType=RunType.download
        };

      var re = new Regex(filterre, RegexOptions.Compiled| RegexOptions.IgnoreCase);
      var files = account.service.fileCache.getContainer(account.id, cont.id, null)
          .Where(x => re.IsMatch(x.path))
          .Select(x => new FileDiff { local=null, remote=x, type=DiffType.created})
          .ToList();

      if (files.Count > 10 && !noAction && account.svcName != "BackupLib.LocalService")
        {
          throw new ArgumentOutOfRangeException("filterre"
            , string.Format("CopyLocal will not copy more than 10 files. This should be used to restore some files, not many. ({0} - {1} #{2})"
            , account.name, cont.name, files.Count));
        }
      
      dp.add(files);
      dp.run();
    }
  }
}

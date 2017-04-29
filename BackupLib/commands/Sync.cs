using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib.commands
{
  using System.Text.RegularExpressions;
  using BUCommon;

  public class Sync :ICommand
  {
    public string helptext => @"sync <account>:<container> <root> [keyfile] [--force-remote] 
         -  compare the local path to the remote path and push changes to
         the remote path.  optional '--force-remote' forces the program
         to pull the contents of the container before sync'ing.
";
    
    public Account account {get;set;}
    public FileCache cache {get;set;}
    public Container container {get;set;}
    public string keyFile {get;set;}
    public string pathRoot {get;set;}
    public bool noAction {get;set;}
    public Action<FileDiff> progress {get;set;}
    public string filterRE {get;set;}
    public string excludeRE {get;set;}

    /// <summary>contact remote for file status?</summary>
    public bool useRemote {get;set;}

    public void run()
    {
      var cmp = (new DirectoryCompare 
        { account=account, cache=cache, container=container, pathRoot=pathRoot, useRemote=useRemote })
        .run();
      
      if (!string.IsNullOrWhiteSpace(filterRE)) 
        { 
          Regex re = new Regex(filterRE, RegexOptions.Compiled| RegexOptions.IgnoreCase); 
          cmp = cmp.Where(x => re.IsMatch(x.local != null ? x.local.path : x.remote.path)).ToList();
        }
      
      if (!string.IsNullOrWhiteSpace(excludeRE))
        {
          Regex reex = new Regex(excludeRE, RegexOptions.Compiled| RegexOptions.IgnoreCase);
          cmp = cmp.Where(x => !reex.IsMatch(x.local != null ? x.local.path : x.remote.path)).ToList();
        }

      /* start with creates, updates, then deletes. */
      var dp = new DiffProcessor 
        { 
          container=container
          , account=account
          , maxTasks=10
          , root=pathRoot
          , encKey=keyFile
          , noAction = noAction
          , progressHandler=progress
          , runType=RunType.upload
        };

      dp.add(cmp.Where(x => x.type == DiffType.created));
      dp.run();

      dp.add(cmp.Where(x => x.type == DiffType.updated));
      dp.run();
      
      dp.add(cmp.Where(x => x.type == DiffType.deleted));
      dp.run();
    }
  }
}

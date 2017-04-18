using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib.commands
{
  public class DirectoryCompare
  {
    public BUCommon.FileCache cache {get;set;}
    public BUCommon.Account account {get;set;}
    public BUCommon.Container container {get;set;}
    public string pathRoot {get;set;}
    public bool useRemote {get;set;}

    public IReadOnlyList<FileDiff> run()
    {
      var ll = new LocalLister();

      var localfiles = ll.getList(pathRoot);

      var remoteFiles = 
        (new FileList 
          { account=account, cache=cache, container=container, versions=false, useRemote=useRemote, pathRE=null }
         ).run();

      var dd = new DirDiff();
      var diffs = dd.compare(localfiles, remoteFiles);

      /* filter out unknowns, sames */
      return diffs
        .Where(x => x.type == DiffType.created || x.type == DiffType.updated || x.type == DiffType.deleted)
        .ToList();
    }
  }
}

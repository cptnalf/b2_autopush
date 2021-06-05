using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib.commands
{
  using System.Text.RegularExpressions;
  
  public class DirectoryCompare
  {
    public BUCommon.FileCache cache {get;set;}
    public BUCommon.Account account {get;set;}
    public BUCommon.Container container {get;set;}
    public string pathRoot {get;set;}
    public bool useRemote {get;set;}

    public string privateKey {get;set;}
    public bool useHash {get;set;}

    public string filter {get;set;}
    public string exclude {get;set;}
    public int maxTasks {get;set;}

    public IReadOnlyList<FileDiff> run()
    {
      Regex reex = null;
      Regex re = null;

      maxTasks = BUCommon.IOUtils.DefaultTasks(maxTasks);

      var ll = new LocalLister();

      var localfiles = ll.getList(pathRoot, exclude, filter);

      var remoteFiles = 
        (new FileList 
          { account=account, cache=cache, container=container, versions=false, useRemote=useRemote, pathRE=null }
         ).run();

      if (!string.IsNullOrWhiteSpace(filter)) 
        { 
          re = new Regex(filter, RegexOptions.Compiled| RegexOptions.IgnoreCase); 
          remoteFiles = remoteFiles.Where(x => re.IsMatch(x.path)).ToList();
        }
      
      if (!string.IsNullOrWhiteSpace(exclude))
        {
          reex = new Regex(exclude, RegexOptions.Compiled| RegexOptions.IgnoreCase);
          remoteFiles = remoteFiles.Where(x => !reex.IsMatch(x.path)).ToList();
        }
      
      var dd = new DirDiff();
      if (useHash)
        {
          dd.usehash = true;
          dd.privateKey = privateKey;
          dd.pathRoot = pathRoot;
        }
      dd.maxTasks = this.maxTasks;

      var diffs = dd.compare(localfiles, remoteFiles);

      /* filter out unknowns, sames */
      return diffs
        .Where(x => x.type == DiffType.created || x.type == DiffType.updated || x.type == DiffType.deleted)
        .ToList();
    }
  }
}

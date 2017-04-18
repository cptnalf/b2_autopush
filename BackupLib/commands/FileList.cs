using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib.commands
{
  public class FileList
  {
    public BUCommon.FileCache cache {get;set;}
    public BUCommon.Account account {get;set;}
    public BUCommon.Container container {get;set;}
    public string pathRE {get;set;}

    public bool useRemote {get;set;}
    /* i'm not really sure why this wouldn't always be true.
     * atleast for backblaze b2. you get a list of files, with upload dates.
     * might not watnt to do the versions always...
     * versions always means i'd need to deal with versioning here too
     * , which could cause more problems than it's worth.
     */
    public bool versions {get;set;}

    public IReadOnlyList<BUCommon.FreezeFile> run()
    {
      if (useRemote) { _remoteQuery(); }
      
      var files = 
          cache.files
            .Where(x =>   
                (   account == null 
                 || (   x.container != null 
                     && account.id == x.container.accountID
                     && (container == null || x.container.id == container.id)
                    )
                )
            );
      if (!string.IsNullOrWhiteSpace(pathRE))
        {
          var re = new System.Text.RegularExpressions.Regex(pathRE, System.Text.RegularExpressions.RegexOptions.Compiled);
          files = files.Where(x => re.IsMatch(x.path));
        }

      return files.ToList();
    }

    private void _remoteQuery()
    {
      if (container == null)
        {
          /* issue a container query first. */
          var cq = new Containers { account=account, cache=cache };
          cq.run();
        }

      var containers = container == null ? cache.containers : cache.containers.Where(x => x.accountID == container.accountID && x.id == container.id);
      foreach(var c in containers)
        {
          IReadOnlyList<BUCommon.FreezeFile> files = null;
          if (versions)
            { files = account.service.getVersions(c); }
          else
            { files = account.service.getFiles(c); }
          
          foreach(var f in files) { cache.add(f); }
        }
    }
  }
}

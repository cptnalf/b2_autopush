using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackupLib
{
  using BUCommon;

  public enum DiffType
  {
    unknown
    ,same
    ,deleted
    ,created
    ,updated
  }

  public class FileDiff
  {
    public FreezeFile file {get;set; }
    public DiffType type {get;set; }
  }

  /// <summary>
  /// take 2 directory listings and produce a list of files that are different.
  /// </summary>
  public class DirDiff
  {
    public IReadOnlyList<FileDiff> compare(IReadOnlyList<FreezeFile> local, IReadOnlyList<FreezeFile> provider)
    {
      List<FileDiff> files = new List<FileDiff>();

      var dels = 
        from pf in provider 
        where local.Where((x) => string.Compare(x.path, pf.path, true) == 0).Any() == false
        select pf;
      foreach(var d in dels) { files.Add(new FileDiff { file=d, type=DiffType.deleted }); }

      var creates =
        from lf in local
        where provider.Where((x) => string.Compare(x.path, lf.path, true) == 0).Any() == false
        select lf;

      foreach(var c in creates) { files.Add(new FileDiff { file=c, type=DiffType.created }); }

      var updates =
        from lf in local
        join pf in provider on lf.path equals pf.path
        where lf.uploaded.Date != pf.uploaded.Date
        select lf;

      foreach(var c in updates) { files.Add(new FileDiff { file=c, type=DiffType.updated }); }

      return files;
    }
  }
}

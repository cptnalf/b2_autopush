﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupLib
{
  using Path = System.IO.Path;
  using File = System.IO.File;
  using FileStream = System.IO.FileStream;
  using FileMode = System.IO.FileMode;
  using FileAccess = System.IO.FileAccess;
  using FileShare = System.IO.FileShare;
  using MemoryStream = System.IO.MemoryStream;
  using StreamReader = System.IO.StreamReader;

  public enum RunType
  {
    none
    ,upload
    ,download
  }

  public class DiffProcessor
  {
    internal class TLocalData
    {
      internal BaseEncrypt fe {get;set;}
      internal object auth {get;set;}
    }
    
    private List<FileDiff> _diffs = new List<FileDiff>();

    public int maxTasks {get;set;}
    public Action<FileDiff,Exception> errorHandler {get;set;}
    public Action<FileDiff> progressHandler {get;set;}
    public BUCommon.Account account {get;set;}
    public BUCommon.Container container {get;set;}
    public string encKey {get;set;}
    public string root {get;set;}
    public bool noAction {get;set;}
    public RunType runType {get;set;}
    public string agePath {get;set;}

    public DiffProcessor() { }

    public void add(FileDiff d) { _diffs.Add(d); }
    public void add(IEnumerable<FileDiff> ds) { _diffs.AddRange(ds); }

    public void run()
    {
      BaseEncryptBuilder bldr = null;
      maxTasks = BUCommon.IOUtils.DefaultTasks(maxTasks);
      if (container.encType == "AGE")
        {
          bldr = new Age.AgeEncryptBuilder { AgePath = this.agePath };
          bldr.init(encKey);          
        }
      else 
        {
          bldr = new FileEncryptBuilder();
          bldr.init(encKey);
        }

      if (noAction) 
        {
          maxTasks = 1;
          runType = RunType.none;
        }

      var service = account.service;
      var cache = service.fileCache;

      var tasks = Parallel.ForEach(_diffs
        ,new ParallelOptions { MaxDegreeOfParallelism=maxTasks}
        ,() => 
        {
          BaseEncrypt fe1 = bldr.build();
          object td = null;
          if (!noAction)
            { td = service.threadStart(); }
          return new TLocalData { fe=fe1, auth=td };
        }
        ,(x,pls,tl) =>
        {
          progressHandler?.Invoke(x);
          
          string path = string.Empty;
          
          FileStream filestrm = null;
          try {
            switch(runType)
              {
              case RunType.upload: 
                {
                  path = x.local.path.Replace('/', Path.DirectorySeparatorChar); 
                  path = Path.Combine(root, path);

                  switch(x.type)
                    {
                      case DiffType.deleted:
                      {
                        service.delete(x.remote);
                        lock(cache) { cache.delete(x.remote); }
                        break;
                      }
                      case DiffType.created:
                      case DiffType.updated:
                      {
                        filestrm = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite|FileShare.Delete);
                        var hash = tl.fe.hashContents(filestrm);
                        /* since we're reading anyways, populate the file hash. */
                        x.local.localHash = hash;
                  
                        var memstrm = tl.fe.encrypt(filestrm);
                        memstrm.Seek(0, System.IO.SeekOrigin.Begin);
                        x.local.localHash = tl.fe.hashContents("SHA1", memstrm);
                        memstrm.Seek(0, System.IO.SeekOrigin.Begin);

                        byte[] buf = x.local.localHash.raw; //tl.fe.encBytes(x.local.localHash.raw);
                        var b64 = Convert.ToBase64String(buf);
                        var ff = service.uploadFile(tl.auth, container, x.local, memstrm, b64);
                        memstrm.Dispose();
                        memstrm = null;

                        if (ff == null)
                          { errorHandler?.Invoke(x, new Exception("Failed to proces!")); }
                        else 
                          {
                            ff.localHash = x.local.localHash;
                            lock(cache) { cache.add(ff); }
                          }
                        break;
                      }
                    }
                   break; 
                }
              case RunType.download: 
                {
                  if (x.type == DiffType.created || x.type== DiffType.updated)
                    {
                      /* this really needs to check to see if we need to download it. 
                       * that will make it resumeable.
                       */
                      path = Path.Combine(root, x.remote.path.Replace('/', Path.DirectorySeparatorChar));

                      {
                        /* need to make sure the download directory parts exist before we download and save the file. */
                        var pathfname = Path.GetFileName(path);
                        var pathpart = path.Substring(0,path.Length - pathfname.Length);
                        System.IO.Directory.CreateDirectory(pathpart);
                      }

                      var strm = service.downloadFile(tl.auth, x.remote);
                      filestrm = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
                      tl.fe.decrypt(strm, filestrm);
                      strm.Dispose();
                      strm = null;

                      var hash = tl.fe.hashContents(filestrm);

                      x.remote.localHash = hash;
                      lock(cache) { cache.add(x.remote); }

                      filestrm.Close();
                      filestrm.Dispose();
                      filestrm = null;
                    }

                  break; 
                }
              case RunType.none: { break; }
              }
          } 
          catch (Exception e)
            {
              errorHandler?.Invoke(x,e);
              pls.Stop();
              throw new ArgumentException(string.Format("Error processing file diff item. {0} - {1}", x.type
                , (x.local != null ? x.local.path : x.remote.path))
                , e);
            }
          return tl;
        }
        ,x => { if (!noAction) { service.threadStop(x.auth); } }
        );
      _diffs.Clear();
    }
  }
}

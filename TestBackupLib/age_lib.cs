#define use_stdin_age
using System;
using System.Diagnostics;
using NUnit.Framework;

namespace TestBackupLib
{
  using Path = System.IO.Path;
  using FileStream = System.IO.FileStream;
  using FileAccess = System.IO.FileAccess;
  using FileMode = System.IO.FileMode;
  using FileShare = System.IO.FileShare;

  public class Starter
  {
    public System.IO.Stream src1 {get;set;}
    public System.IO.Stream dest1 {get;set;}
    public System.IO.Stream src2 {get;set;}
    public System.IO.Stream dest2 {get;set;}
  }

  [TestFixture]
  [Category("TEST_AGE")]
  public class AgeTesting
  {
    [Test]
    public async System.Threading.Tasks.Task AgeBasic()
    {
      var fname = _writeRecipients();
      var srcfile = "/data2/photos/source/alpha7/2020/09-oregon/0930/DSC00773.ARW";

      var p = new Process();
      p.StartInfo = new ProcessStartInfo();
      p.StartInfo.WorkingDirectory = "/data2/temp";
      p.StartInfo.FileName = "/home/chiefengineer/releases/age/age";
#if use_stdin_age
      p.StartInfo.Arguments = string.Format("-R {0}", fname);
#else
      p.StartInfo.Arguments = string.Format("-R {0} {1}", fname, srcfile);
#endif
      p.StartInfo.UseShellExecute = false;
      p.StartInfo.ErrorDialog = false;
      p.StartInfo.RedirectStandardInput = true;
      p.StartInfo.RedirectStandardOutput = true;

      p.Start();
#if use_stdin_age
      var src = new FileStream(srcfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite|FileShare.Delete);
#endif
      var dest = new FileStream("/data2/temp/b2app.test.foo.age", FileMode.Create, FileAccess.Write, FileShare.ReadWrite|FileShare.Delete);

#if false
      TestContext.Progress.WriteLine("dump src to dest via copyto");
      await src.CopyToAsync(p.StandardInput.BaseStream);
#endif
      System.Threading.Thread t = null;
      System.Threading.Thread t2 = null;
      {
        t =new System.Threading.Thread(_FooStart);
        var obj = new Starter
          {
            src1 = src
            ,dest1 = p.StandardInput.BaseStream
          };
        t.Start(obj);
      }
      {
        t2 =new System.Threading.Thread(_FooStart);
        var obj = new Starter
          {
            src1 = p.StandardOutput.BaseStream
            ,dest1 = dest
          };
        t2.Start(obj);
      }

      //await p.StandardOutput.BaseStream.CopyToAsync(dest);

/*
      {
        while(true)
          {
            
            var str = await p.StandardError.ReadLineAsync();
            if (str != null) { await TestContext.Progress.WriteAsync(str); }

            if (p.StandardError.EndOfStream || str == null) { break; }
          }
      }
      */
      p.WaitForExit();
      t.Join();
      t2.Join();
      System.IO.File.Delete(fname);
    }

    private static void _FooStart(object args)
    {
      var b1 = new byte[16384];
      var m1 = new Memory<byte>(b1);
      var strms = args as Starter;
      long l1 = 0;

      while(true)
        {
          if (strms == null || (strms.dest1 == null && strms.src2 == null)) { break; }

          if (strms.dest1 != null)
            {
              TestContext.Progress.WriteLine("write stdin.");
              l1 = strms.src1.ReadAsync(m1).Result;
              if (l1 > 0) { strms.dest1.WriteAsync(m1).GetAwaiter().GetResult(); }

              if (l1 == 0)
                {
                  strms.dest1.Flush();
                  strms.dest1.Close();
                  strms.dest1 = null;
                  strms.src1.Close();
                  strms.src1 = null;
                }
            }
        }
    }

    private string _writeRecipients()
    {
      var str = "/data2/temp/age_key.txt";

      var tmpname = System.IO.Path.GetRandomFileName();

      var p = new Process();
      p.StartInfo = new ProcessStartInfo("/home/chiefengineer/releases/age/age-keygen", string.Format("-y {0}", str));
      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardOutput = true;

      p.Start();
      p.WaitForExit();
      var strm = new FileStream(Path.Combine("/data2/temp/", tmpname), FileMode.CreateNew, FileAccess.Write, FileShare.Delete|FileShare.ReadWrite);

      p.StandardOutput.BaseStream.CopyTo(strm);
      strm.Close();
      strm = null;

      return Path.Combine("/data2/temp/", tmpname);
    }
  }
}
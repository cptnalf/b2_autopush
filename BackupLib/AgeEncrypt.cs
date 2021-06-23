using System;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace BackupLib.Age
{
  /// encryption using 'age' external program.
  public class AgeEncrypt : BaseEncrypt
  {
    internal class Starter
    {
      public Stream src {get;set;}
      public Stream dest {get;set;}
      public bool noclosedest {get;set;}
    }

    internal const string AGE_BIN_NAME = "age";
    internal const string AGEKEYGEN_BIN_NAME = "age-keygen";

    public string ReceipientFile {get;set;}
    public string AgePath {get;set;}
    public string WorkingDir {get;set;}

    public AgeEncrypt() { }

    public void writeRecipients(string srcKey)
    {
      var p = new Process();
      p.StartInfo = new ProcessStartInfo(Path.Combine(this.AgePath, AGEKEYGEN_BIN_NAME), string.Format("-y {0}", srcKey));
      p.StartInfo.UseShellExecute = false;
      p.StartInfo.RedirectStandardOutput = true;

      p.Start();
      p.WaitForExit();
      var strm = new FileStream(ReceipientFile, FileMode.CreateNew, FileAccess.Write, FileShare.Delete|FileShare.ReadWrite);

      p.StandardOutput.BaseStream.CopyTo(strm);
      strm.Close();
      strm = null;
    }

    public override MemoryStream encrypt(Stream instrm)
    {
      var dest = new MemoryStream();
      _runAge(string.Format("-e -R {0}", this.ReceipientFile), instrm, dest);
      
      dest.Seek(0, System.IO.SeekOrigin.Begin);

      return dest;
    }

    public override void decrypt(Stream instrm, FileStream strm)
    {
      _runAge(string.Format("-d -i {0}", this.ReceipientFile), instrm, strm);
    }

    private void _runAge(string args, Stream from, Stream to)
    {
      var p = new Process();
      p.StartInfo = new ProcessStartInfo();
      p.StartInfo.WorkingDirectory = this.WorkingDir; //"/data2/temp";
      p.StartInfo.FileName = Path.Combine(this.AgePath, AGE_BIN_NAME); //"/home/chiefengineer/releases/age/age";
      p.StartInfo.Arguments = args;

      p.StartInfo.UseShellExecute = false;
      p.StartInfo.ErrorDialog = false;
      p.StartInfo.RedirectStandardInput = true;
      p.StartInfo.RedirectStandardOutput = true;

      var sw = new System.Diagnostics.Stopwatch();
      sw.Start();

      p.Start();

      System.Threading.Thread tIn = null;
      System.Threading.Thread tOut = null;
      {
        tIn =new System.Threading.Thread(_ThreadStart);
        var obj = new Starter
          {
            src = from
            ,dest = p.StandardInput.BaseStream
            ,noclosedest = false
          };
        tIn.Start(obj);
      }
      {
        tOut =new System.Threading.Thread(_ThreadStart);
        var obj = new Starter
          {
            src = p.StandardOutput.BaseStream
            ,dest = to
            ,noclosedest = true
          };
        tOut.Start(obj);
      }

      p.WaitForExit();
      tIn.Join();
      tOut.Join();
    }

    private static void _ThreadStart(object args)
    {
      var strms = args as Starter;

      strms.src.CopyTo(strms.dest);
      strms.dest.Flush();

      if (!strms.noclosedest)
        {
          strms.dest.Close();
          strms.src.Close();
          strms.dest = null;
        }
      strms.src = null;
    }
  }
}
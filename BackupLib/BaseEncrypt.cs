namespace BackupLib
{
  using FileStream = System.IO.FileStream;
  using MemoryStream = System.IO.MemoryStream;
  using Stream = System.IO.Stream;

  public abstract class BaseEncrypt
  {
    

    /// <summary>
    /// encrypt the contents of the provided stream.
    /// </summary>
    /// <param name="instrm"></param>
    /// <returns>memory stream containing the encrypted file</returns>
    public abstract MemoryStream encrypt(Stream instrm);

    /// <summary>
    /// decrypt the contents of an encrypted file.
    /// </summary>
    /// <param name="instrm">the encrypted file contents (all)</param>
    /// <param name="strm">place to put the decrpyted file</param>
    public void decrypt(byte[] instrm, FileStream strm)
    { 
      var mstrm = new MemoryStream(instrm);
      decrypt(mstrm, strm);
      mstrm.Dispose();
      mstrm = null;
    }

    /// <summary>
    /// decrypt the contents of and encrypted file.
    /// </summary>
    /// <param name="instrm">encrypted file</param>
    /// <param name="strm">output file for decrypted contents.</param>
    /// <remarks>
    /// the output stream needs both read and write access 
    /// since the contents will be written to it,
    /// and then the contents will be verified via the hash
    /// embeded in the encrypted file's header.
    /// </remarks>
    public abstract void decrypt(Stream instrm, FileStream strm);

  }
}
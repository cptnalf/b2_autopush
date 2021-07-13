namespace BackupLib
{
  using System.Security.Cryptography;
  using FileStream = System.IO.FileStream;
  using MemoryStream = System.IO.MemoryStream;
  using Stream = System.IO.Stream;
  using SeekOrigin = System.IO.SeekOrigin;

  public abstract class BaseEncryptBuilder
  {
    public abstract void init(string keyfile);
    public abstract BaseEncrypt build();
  }

  public abstract class BaseEncrypt
  {
     /// <summary>
    /// computes the hash of the given stream.
    /// </summary>
    /// <param name="strm"></param>
    /// <returns></returns>
    public BUCommon.Hash hashContents(Stream strm)
    {
      var hasha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
      strm.Seek(0, SeekOrigin.Begin);
      var res = _computeHash(strm, hasha);
      hasha.Dispose();
      strm.Seek(0, SeekOrigin.Begin);
      return BUCommon.Hash.Create(HashAlgorithmName.SHA256.Name, res);
    }

    /// <summary>
    /// computes the hash of the given stream.
    /// </summary>
    /// <param name="strm"></param>
    /// <returns></returns>
    public BUCommon.Hash hashContents(string hashAlgo, Stream strm)
    {
      var algo = HashAlgorithmName.MD5;
      if (hashAlgo == HashAlgorithmName.SHA1.Name) { algo = HashAlgorithmName.SHA1; }
      if (hashAlgo == HashAlgorithmName.SHA256.Name) { algo = HashAlgorithmName.SHA256; }
      if (hashAlgo == HashAlgorithmName.SHA384.Name) { algo = HashAlgorithmName.SHA384; }
      if (hashAlgo == HashAlgorithmName.SHA512.Name) { algo = HashAlgorithmName.SHA512; }
      
      var hasha = IncrementalHash.CreateHash(algo);
      strm.Seek(0, SeekOrigin.Begin);
      var res = _computeHash(strm, hasha);
      hasha.Dispose();
      strm.Seek(0, SeekOrigin.Begin);
      return BUCommon.Hash.Create(algo.Name, res);
    }

 
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


    /// <summary>
    /// compute the hash of a stream
    /// </summary>
    /// <param name="strm"></param>
    /// <param name="hash"></param>
    /// <returns>hash bytes</returns>
    /// <remarks>
    /// this attempts to return the stream to the position it started from
    /// this makes it easy to compute the hash of a file, then directly
    /// read from the file for encryption/transmission/etc.
    /// </remarks>
    protected byte[] _computeHash(Stream strm, IncrementalHash hash)
    {
      if (!strm.CanRead) { return null; }
      long curpos = strm.Position;

      _writeBytesToHash(strm, hash);
      var hashoenc = hash.GetHashAndReset();

      if (strm.CanSeek) { strm.Seek(curpos, System.IO.SeekOrigin.Begin); }

      return hashoenc;
    }

    private void _writeBytesToHash(Stream strm, IncrementalHash hash)
    {
      if (!strm.CanRead) { return; }

      byte[] buffer = new byte[100 * 1024];
      int lread = 0;
      do {
        lread = strm.Read(buffer, 0, buffer.Length);
        if (lread > 0)
          { hash.AppendData(buffer,0, lread); }
      } while(lread > 0);      
    }
  }
}
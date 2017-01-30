using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackupLib
{
  using System.Security.Cryptography;
  using Stream = System.IO.Stream;
  using FileStream = System.IO.FileStream;
  using MemoryStream = System.IO.MemoryStream;
  using SeekOrigin = System.IO.SeekOrigin;
  using Encoding = System.Text.Encoding;
  using FileMode = System.IO.FileMode;
  using FileAccess = System.IO.FileAccess;
  using FileShare = System.IO.FileShare;

  /* file format is:
    * [16 + 96 + 3? = 115]
    * [encrypted block]
    * 4[sha256 = 256bits = 32 bytes] original file content hash.
    * 4[3 bytes?] enc algo
    * 4[256 bits = 32 bytes] key
    * 4[256bits = 32 bytes] iv
    * [end encrypted block]
    * [size] signed encrypted content hash
    * encrypted content.
    */
  
  public class FileEncrypt
  {
    private SymmetricAlgorithm _algo;
    private HashAlgorithmName _hash;
    private RSA _rsa;
    
    public FileEncrypt(RSA rsa) { _rsa = rsa; }

    public MemoryStream encrypt(Stream instrm)
    {
      var strm = new MemoryStream();
      var incHash = IncrementalHash.CreateHash(_hash);

      _algo = Aes.Create();
      {
        /* setup the algorythm. */
        var foo = RandomNumberGenerator.Create();
        byte[] arr = new byte[_algo.KeySize / 8];

        foo.GetBytes(arr);
        _algo.Key = arr;
        arr = new byte[_algo.BlockSize / 8];
        foo.GetBytes(arr);
        _algo.IV = arr;
      }
      
      {
        /* dump the encrypted block data into the block. */
        var data = new MemoryStream();
        var bytes = _computeHash(instrm, incHash);
        _writeBytes(data, bytes);

        _writeBytes(data, System.Text.Encoding.UTF8.GetBytes("AES"));
        _writeBytes(data, _algo.Key);
        _writeBytes(data, _algo.IV);

        var enc = _rsa.Encrypt(data.ToArray(), RSAEncryptionPadding.OaepSHA1);

        _writeBytes(strm, enc);
        data.Dispose();
        data = null;
      }

      /* i want the encrypted block along with the encrypted data to be signed. */
      {
        var encfile = new MemoryStream();
        _processCryptoStream(instrm, _algo.CreateEncryptor(), strm);

        encfile.Seek(0, SeekOrigin.Begin);

        strm.Seek(0, SeekOrigin.Begin);
        _writeBytesToHash(strm, incHash);

        var hash = _computeHash(encfile, incHash);
        var sig = _rsa.SignHash(hash, _hash, RSASignaturePadding.Pkcs1);
        _writeBytes(strm, sig);

        _writeStream(encfile, strm);
      }

      strm.Seek(0, SeekOrigin.Begin);
      return strm;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instrm">encrypted file</param>
    /// <param name="strm">output file for decrypted contents.</param>
    public void decrypt(MemoryStream instrm, FileStream strm)
    {
      byte[] origHash = null;
      var incHash = IncrementalHash.CreateHash(_hash);

      var enchdr = _readBytes(instrm);

      {
        /* verify signature. */
        var sig = _readBytes(instrm);

        incHash.AppendData(enchdr);
        var enchash = _computeHash(instrm, incHash);
        var sigisok = _rsa.VerifyHash(enchash, sig, _hash, RSASignaturePadding.Pkcs1);

        if (!sigisok) { throw new System.Security.SecurityException("Hash Signature validation failed."); }
      }
      
      {
        var dec = _rsa.Decrypt(enchdr, RSAEncryptionPadding.OaepSHA1);
        var decStrm = new MemoryStream(dec);
        
        origHash = _readBytes(decStrm);
        string algoname = Encoding.UTF8.GetString(_readBytes(decStrm));

        if (algoname != "AES") { throw new ArgumentException("Symetric encryption algo is not AES?"); }
        _algo = Aes.Create();
        _algo.Key = _readBytes(decStrm);
        _algo.IV = _readBytes(decStrm);
      }
      
      _processCryptoStream(instrm, _algo.CreateDecryptor(), strm);

      if (strm.CanRead)
        {
          /* verify output file. */
          strm.Seek(0, SeekOrigin.Begin);
          var writtenhash = _computeHash(strm, incHash);
          for(int i=0; i< writtenhash.Length; ++i) 
            { if (origHash[i] != writtenhash[i]) { throw new ArgumentException("Written data does not match encrypted data."); } }
        }
      strm.Dispose();
      strm = null;
    }

    private byte[] _readBytes(Stream strm)
    {
      byte[] size = new byte[4];
      int len = 0;
      len = strm.Read(size,0, 4);

      if (len < size.Length) { throw new ArgumentOutOfRangeException("Can't read size of field from file."); }
      len = BitConverter.ToInt32(size,0);
      size = null;

      byte[] data = new byte[len];
      len = strm.Read(data, 0, data.Length);
      if (len < data.Length) 
        { throw new ArgumentOutOfRangeException(string.Format("Can't read data. got {0} of {1} bytes", len, data.Length)); }

      return data;
    }

    private void _writeBytes(Stream strm, byte[] bytes) { _writeBytes(strm, bytes, bytes.Length); }

    private void _writeBytes(Stream strm, byte[] bytes, int length)
    {
      byte[] lenk = BitConverter.GetBytes(bytes.Length);
      
      strm.Write(lenk, 0, lenk.Length);
      strm.Write(bytes, 0, bytes.Length);
    }

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
    private byte[] _computeHash(Stream strm, IncrementalHash hash)
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

    private void _processCryptoStream(Stream inbytes, ICryptoTransform xform, Stream outbytes)
    {
      var cryptostrm = new CryptoStream(outbytes, xform, CryptoStreamMode.Write);

      _writeStream(inbytes, cryptostrm, xform.OutputBlockSize * 10);

      cryptostrm.FlushFinalBlock();
      cryptostrm.Dispose();
    }

    private void _writeStream(Stream instrm, Stream outStrm) { _writeStream(instrm, outStrm, 100 * 1024); }

    private void _writeStream(Stream instrm, Stream outStrm, int bufSize)
    {
      if (!instrm.CanRead) { return; }
      if (!outStrm.CanWrite) { return; }

      int read = 0;
      byte[] block = new byte[bufSize];
      
      do {
        read = instrm.Read(block, 0, block.Length);
        if (read > 0) { outStrm.Write(block, 0, read); }
      } while(read > 0);
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackupLib
{
  using System.Security.Cryptography;
  using Stream = System.IO.Stream;
  using FileStream = System.IO.FileStream;
  using StreamReader = System.IO.StreamReader;
  using MemoryStream = System.IO.MemoryStream;
  using SeekOrigin = System.IO.SeekOrigin;
  using Encoding = System.Text.Encoding;
  using FileMode = System.IO.FileMode;
  using FileAccess = System.IO.FileAccess;
  using FileShare = System.IO.FileShare;

  /* file format is:
    * [16 + 64 + 3? = 83]
    * [encrypted block]
    * 4[sha256 = 256bits = 32 bytes] original file content hash.
    * 4[3 bytes?] enc algo
    * 4[128 bits = 16 bytes] key
    * 4[128bits = 16 bytes] iv
    * [end encrypted block]
    * [size] signed encrypted content hash
    * encrypted content.
    */
  
  public class FileEncryptBuilder : BaseEncryptBuilder
  {
    private byte[] _cont;

    public override void init(string keyFile)
    {
      var kt = new MemoryStream();
      var fs = new FileStream(keyFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      var len = BUCommon.IOUtils.WriteStream(fs,kt).Result;
      fs.Close();
      fs.Dispose();
      fs = null;

      _cont = kt.ToArray();
    }

    public override BaseEncrypt build()
    {
      var sr = new StreamReader(new MemoryStream(_cont, 0, _cont.Length, false, false));
      var rsa = KeyLoader.LoadRSAKey(sr);

      var fe1 = new FileEncrypt(rsa);
      sr = null; 

      return fe1;
    }

  }

  public class FileEncrypt : BaseEncrypt
  {
    internal class FileHeader
    {
      internal static FileHeader Create(byte[] buffer)
      {
        FileHeader res = new FileHeader();

        int pos = 0;
        byte[] ab = _Bytes(buffer, pos);
        res.hashname = Encoding.UTF8.GetString(ab);
        pos += ab.Length + 4;
        res.hash = _Bytes(buffer, pos);
        pos += res.hash.Length + 4;

        ab = _Bytes(buffer, pos);
        res.algo = Encoding.UTF8.GetString(ab);
        pos += ab.Length + 4;
        res.key = _Bytes(buffer,pos);
        pos += res.key.Length + 4;
        res.iv = _Bytes(buffer,pos);

        return res;
      }

      internal string hashname;
      internal byte[] hash;
      internal string algo;
      internal byte[] key;
      internal byte[] iv;

      internal byte[] toBytes()
      {
        var strm = new System.IO.MemoryStream();

        byte[] len;
        byte[] ab = Encoding.UTF8.GetBytes(hashname);
        len = BitConverter.GetBytes(ab.Length);
        strm.Write(len, 0, 4);
        strm.Write(ab, 0, ab.Length);
        
        len = BitConverter.GetBytes(hash.Length);
        strm.Write(len, 0, 4);
        strm.Write(hash,0, hash.Length);

        ab = Encoding.UTF8.GetBytes(algo);
        len = BitConverter.GetBytes(ab.Length);
        strm.Write(len, 0, 4);
        strm.Write(ab, 0, ab.Length);

        len = BitConverter.GetBytes(key.Length);
        strm.Write(len, 0, 4);
        strm.Write(key, 0, key.Length);

        len = BitConverter.GetBytes(iv.Length);
        strm.Write(len, 0, 4);
        strm.Write(iv, 0, iv.Length);

        return strm.ToArray();
      }

      private static byte[] _Bytes(byte[] buffer, int pos)
      {
        int size;

        size = BitConverter.ToInt32(buffer,pos);
        byte[] res = new byte[size];
        pos += 4;
        for(int i=0; i < res.Length; ++i) { res[i] = buffer[i + pos]; }

        return res;
      }
    }

    private SymmetricAlgorithm _algo;
    private HashAlgorithmName _hash;
    private RSA _rsa;
    private RandomNumberGenerator _rng;
    
    /// <summary>
    /// construct a file encrypter or decrpyter
    /// using the public key, we get an encryptor
    /// using the private key, we get a decryptor
    /// </summary>
    /// <param name="rsa"></param>
    public FileEncrypt(RSA rsa)
    {
      _rsa = rsa; 
      _rng = RandomNumberGenerator.Create();
      _hash = HashAlgorithmName.SHA256;

      _algo = null;
    }

    public byte[] encBytes(byte[] foo)
    {
      if (foo.Length > _rsa.KeySize) 
        { 
          throw new ArgumentOutOfRangeException(string.Format("bytes ({0}) are bigger than keysize ({1})"
            , foo.Length, _rsa.KeySize));
        }
      return _rsa.Encrypt(foo, RSAEncryptionPadding.OaepSHA1);
    }
    public byte[] decBytes(byte[] foo)
    { return _rsa.Decrypt(foo, RSAEncryptionPadding.OaepSHA1); }

    /// <summary>
    /// encrypt the contents of the provided stream.
    /// </summary>
    /// <param name="instrm"></param>
    /// <returns>memory stream containing the encrypted file</returns>
    public override MemoryStream encrypt(Stream instrm)
    {
      var strm = new MemoryStream();
      _hash = HashAlgorithmName.SHA256;
      var incHash = IncrementalHash.CreateHash(_hash);

      _algo = Aes.Create();
      
      _algo.Mode = CipherMode.CBC;
      _algo.Padding = PaddingMode.PKCS7;
      _algo.KeySize = 128;
      {
        /* setup the algorythm. */
        byte[] arr = new byte[_algo.KeySize / 8];

        _rng.GetBytes(arr);

        _algo.Key = arr;
        arr = new byte[_algo.BlockSize / 8];
        _rng.GetBytes(arr);
        _algo.IV = arr;
      }
      
      string b64;
      {
        /* dump the encrypted block data into the block. */
        var hdr = new FileHeader();
        hdr.hashname = _hash.Name;
        hdr.hash = _computeHash(instrm, incHash);
        hdr.algo = "AES";
        hdr.key = _algo.Key;
        hdr.iv = _algo.IV;
        
        b64 = Convert.ToBase64String(hdr.hash);

        byte[] hdrbytes = hdr.toBytes();

        var enc = _rsa.Encrypt(hdrbytes, RSAEncryptionPadding.OaepSHA1);

        _writeBytes(strm, enc);
      }

      {
        var encstrm = new MemoryStream();
        var xform = _algo.CreateEncryptor();
        var cryptostrm = new CryptoStream(encstrm, xform, CryptoStreamMode.Write);
        _writeStream(instrm, cryptostrm, xform.OutputBlockSize * 10);
        if (!cryptostrm.HasFlushedFinalBlock) { cryptostrm.FlushFinalBlock(); }
        {
          var enc = encstrm.ToArray();
          strm.Write(enc, 0, enc.Length);
        }
        /* when this happens, the stream is no more. */
        cryptostrm.Dispose();
      }

      strm.Seek(0, SeekOrigin.Begin);
      return strm;
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
    public override void decrypt(Stream instrm, FileStream strm)
    {
      byte[] origHash = null;
      IncrementalHash incHash;

      if (! strm.CanRead) { throw new ArgumentException("Expected to be able to read from output file to verify hash"); }

      var enchdr = _readBytes(instrm);

#if false
      {
        /* verify signature. */
        var sig = _readBytes(instrm);

        incHash.AppendData(enchdr);
        var enchash = _computeHash(instrm, incHash);
        var sigisok = _rsa.VerifyHash(enchash, sig, _hash, RSASignaturePadding.Pkcs1);

        if (!sigisok) { throw new System.Security.SecurityException("Hash Signature validation failed."); }
      }
#endif
      
      {
        var dec = _rsa.Decrypt(enchdr, RSAEncryptionPadding.OaepSHA1);
        var hdr = FileHeader.Create(dec);

        HashAlgorithmName[] names = new HashAlgorithmName[] { HashAlgorithmName.MD5, HashAlgorithmName.SHA1, HashAlgorithmName.SHA256 };
        foreach(var n in names) { if (n.Name == hdr.hashname) { _hash = n; } }
        origHash = hdr.hash;
        incHash = IncrementalHash.CreateHash(_hash);

        if (hdr.algo == "AES")
          {
            _algo = Aes.Create();
            _algo.Mode = CipherMode.CBC;
            _algo.Padding = PaddingMode.PKCS7;

            _algo.KeySize = hdr.key.Length * 8;

            _algo.Key = hdr.key;
            _algo.IV = hdr.iv;
          }
      }
      
      var xform = _algo.CreateDecryptor();
      var cryptostrm = new CryptoStream(instrm, xform, CryptoStreamMode.Read);
      cryptostrm.CopyTo(strm);
      cryptostrm.Dispose();
      {
        /* verify output file. */
        strm.Seek(0, SeekOrigin.Begin);
        var writtenhash = _computeHash(strm, incHash);
        for(int i=0; i< writtenhash.Length; ++i) 
          { if (origHash[i] != writtenhash[i]) { throw new ArgumentException("Written data does not match encrypted data."); } }
      }
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
      
      strm.Write(lenk, 0, 4);
      strm.Write(bytes, 0, bytes.Length);
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

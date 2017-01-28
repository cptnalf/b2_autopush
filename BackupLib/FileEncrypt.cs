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

  public class FileEncrypt
  {
    private RSA _rsa;
    private string _asymKeyFile;
    
    public string asymKeyFile {get { return _asymKeyFile; }  set { _asymKeyFile = value; }}

    public void init()
    {
      /*
      var cp = new CspParameters();
      cp.ProviderType = 1;
      cp.Flags = CspProviderFlags.UseArchivableKey;
      _rsa = new RSACryptoServiceProvider();
      var pkstr = System.IO.File.ReadAllText("c:\\tmp\\rsa_privatekey.xml");
      //"c:\\tmp\\rsa_pubkey.xml");

      _rsa.FromXmlString(pkstr);
      */

      _rsa = RSA.Create();
      _rsa.ImportParameters()
      

    }

    public MemoryStream encrypt(FileStream instrm)
    {
      var foo = System.Security.Cryptography.RandomNumberGenerator.Create();
      byte[] bs = new byte[256];
      foo.GetBytes(bs);
      
      /* hash original contents.
       * encrypt that hash with the public key.
       * that's now an attribute on the files.
       */
      MemoryStream strm = new MemoryStream();
      var sha = SHA384.Create();

      var hash = sha.ComputeHash(instrm);

      Aes aes = Aes.Create();
      aes.KeySize = 256;
      bs = new byte[aes.KeySize / 8];
      foo.GetBytes(bs);
      aes.Key = bs;

      bs = new byte[aes.BlockSize / 8];
      foo.GetBytes(bs);
      aes.IV = bs;
      
      var res = _rsa.Encrypt(hash, RSAEncryptionPadding.OaepSHA256);

      _writeBytes(strm, res);

      res = _rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);
      _writeBytes(strm, res);
      
      res = _rsa.Encrypt(aes.IV, RSAEncryptionPadding.OaepSHA256);
      _writeBytes(strm, res);

      instrm.Seek(0, System.IO.SeekOrigin.Begin);

      /* another memory stream :/ */
      var encfile = new MemoryStream();
      
      {
        var cryptostrm = new CryptoStream(encfile, aes.CreateEncryptor(), CryptoStreamMode.Write);
        byte[] blocksize = new byte[aes.BlockSize];
        int read = 0;
      
        do {
          read = instrm.Read(blocksize,0, blocksize.Length);
          if (read > 0)
            { cryptostrm.Write(blocksize, 0, read); }
        } while(read > 0);
        cryptostrm.FlushFinalBlock();
        cryptostrm.Dispose();
      }
      encfile.Seek(0, System.IO.SeekOrigin.Begin);
      hash = sha.ComputeHash(encfile);

      var han = HashAlgorithmName.SHA256;
      var sighash = _rsa.SignHash(hash, han, RSASignaturePadding.Pkcs1);
      _writeBytes(strm, hash);
      _writeBytes(strm, sighash);

      {
        int read = 0;
        byte[] block = new byte[100*1024];
        encfile.Seek(0, System.IO.SeekOrigin.Begin);

        do {
          read = encfile.Read(block, 0, block.Length);
          if (read > 0) { _writeBytes(strm, block, read); }
        } while(read > 0);
      }

      strm.Seek(0, System.IO.SeekOrigin.Begin);
      return strm;
    }

    public void _writeBytes(Stream strm, byte[] bytes) { _writeBytes(strm, bytes, bytes.Length); }

    public void _writeBytes(Stream strm, byte[] bytes, int length)
    {
      byte[] lenk = BitConverter.GetBytes(bytes.Length);
      
      strm.Write(lenk, 0, lenk.Length);
      strm.Write(bytes, 0, bytes.Length);
    }
  }
}

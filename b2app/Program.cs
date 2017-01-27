using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace b2app
{
  class Program
  {
    private RSACryptoServiceProvider _rsa;
    private string _encrFolder = "c:\\tmp\\enc";
    private string _decrFolder = "c:\\tmp\\dec";
    private string _srcFolder = "c:\\tmp\\src";
    private CspParameters _cspp = new CspParameters(1);

    static void Main(string[] args)
    {
      
      var opts = new B2Net.Models.B2Options();
      opts.AccountId = "";
      opts.ApplicationKey = "";

      var x = new B2Net.B2Client(opts);
      var autht = x.Authorize().Result;
      
      var blst = x.Buckets.GetList().Result;

      var bkt = blst.FirstOrDefault();
      var flst = x.Files.GetList(bkt.BucketId);

      /* list dir.
       * put files into various parts.
       */
      /*
      var aes = Aes.Create();
      var enc = aes.CreateEncryptor(aes.Key,aes.IV);

      var rsa = RSA.Create();
      var rsaparams = new RSAParameters();
      rsa.ImportParameters(rsaparams);
      */

      var p = new Program();
      p._loadKey("c:\\tmp\\id_rsa_1", string.Empty);
    }

    private void _loadKey(string file, string pw)
    {
      AsymmetricCipherKeyPair p;
      var pemrdr = new Org.BouncyCastle.OpenSsl.PemReader(new System.IO.StreamReader(file));
      var o = pemrdr.ReadObject();
      p = (AsymmetricCipherKeyPair)o;

      var rkp = (Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters)p.Private;
      var rku = (Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters)p.Public;
      var rsaparams = new RSAParameters();
      rsaparams.Modulus = rku.Modulus.ToByteArray();
      rsaparams.Exponent = rku.Exponent.ToByteArray();

      var rsa = RSA.Create();
      rsa.ImportParameters(rsaparams);
      rsa.Dispose();
      rsa = null;
      
/*      var cp = new CspParameters();
      cp.ProviderType = 1;
      cp.Flags = CspProviderFlags.UseArchivableKey;
      _rsa = new RSACryptoServiceProvider(cp);

      var pkf = new System.IO.StreamWriter("c:\\tmp\\rsa_pubkey.xml");
      var pvkf = new System.IO.StreamWriter("c:\\tmp\\rsa_privatekey.xml");
      pkf.Write(_rsa.ToXmlString(true));
      pkf.Close();
      pkf.Dispose();

      pvkf.Write(_rsa.ToXmlString(false));
      pvkf.Close();
      pvkf.Dispose();
*/
      var cp = new CspParameters();
      cp.ProviderType = 1;
      cp.Flags = CspProviderFlags.UseArchivableKey;
      _rsa = new RSACryptoServiceProvider();
      var pkstr = System.IO.File.ReadAllText("c:\\tmp\\rsa_pubkey.xml");

      _rsa.FromXmlString(pkstr);

      var res = _rsa.Encrypt(new byte[] {65,66,67}, false);
      var r2 = _rsa.Decrypt(res, false);
    }

    private void EncryptFile(string inFile)
    {
      // Create instance of Rijndael for
      // symetric encryption of the data.
      AesManaged rjndl = new AesManaged();
      rjndl.KeySize = 256;
      rjndl.BlockSize = 256;
      rjndl.Mode = CipherMode.CBC;
      ICryptoTransform transform = rjndl.CreateEncryptor();

      // Use RSACryptoServiceProvider to
      // enrypt the Rijndael key.
      // rsa is previously instantiated: 
      //    rsa = new RSACryptoServiceProvider(cspp);
      byte[] keyEncrypted = _rsa.Encrypt(rjndl.Key, false);

      // Create byte arrays to contain
      // the length values of the key and IV.
      byte[] LenK = new byte[4];
      byte[] LenIV = new byte[4];

      int lKey = keyEncrypted.Length;
      LenK = BitConverter.GetBytes(lKey);
      int lIV = rjndl.IV.Length;
      LenIV = BitConverter.GetBytes(lIV);

      // Write the following to the FileStream
      // for the encrypted file (outFs):
      // - length of the key
      // - length of the IV
      // - ecrypted key
      // - the IV
      // - the encrypted cipher content

      int startFileName = inFile.LastIndexOf("\\") + 1;
      // Change the file's extension to ".enc"
      string outFile = System.IO.Path.Combine(_encrFolder,inFile.Substring(startFileName, inFile.LastIndexOf(".")- startFileName) + ".enc");

      using (System.IO.FileStream outFs = new System.IO.FileStream(outFile, System.IO.FileMode.Create))
      {
        outFs.Write(LenK, 0, 4);
        outFs.Write(LenIV, 0, 4);
        outFs.Write(keyEncrypted, 0, lKey);
        outFs.Write(rjndl.IV, 0, lIV);

        // Now write the cipher text using
        // a CryptoStream for encrypting.
        using (CryptoStream outStreamEncrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
        {
          // By encrypting a chunk at
          // a time, you can save memory
          // and accommodate large files.
          int count = 0;
          int offset = 0;

          // blockSizeBytes can be any arbitrary size.
          int blockSizeBytes = rjndl.BlockSize / 8;
          byte[] data = new byte[blockSizeBytes];
          int bytesRead = 0;

          using (System.IO.FileStream inFs = new System.IO.FileStream(inFile, System.IO.FileMode.Open))
          {
            do
            {
              count = inFs.Read(data, 0, blockSizeBytes);
              offset += count;
              outStreamEncrypted.Write(data, 0, count);
              bytesRead += blockSizeBytes;
            }
            while (count > 0);
            inFs.Close();
          }
          outStreamEncrypted.FlushFinalBlock();
          outStreamEncrypted.Close();
        }

        outFs.Close();
      }
    }
    /*
    private void buttonDecryptFile_Click(object sender, EventArgs e)
    {
      if (_rsa == null)
        { Console.WriteLine("Key not set."); }
      else
        {
          // Display a dialog box to select the encrypted file.
          openFileDialog2.InitialDirectory = EncrFolder;
          if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
              string fName = openFileDialog2.FileName;
              if (fName != null)
                {
                  FileInfo fi = new FileInfo(fName);
                  string name = fi.Name;
                  DecryptFile(name);
                }
            }
        }
    }
    */

    private void DecryptFile(string inFile)
    {
      // Create instance of Rijndael for
      // symetric decryption of the data.
      RijndaelManaged rjndl = new RijndaelManaged();
      rjndl.KeySize = 256;
      rjndl.BlockSize = 256;
      rjndl.Mode = CipherMode.CBC;

      // Create byte arrays to get the length of
      // the encrypted key and IV.
      // These values were stored as 4 bytes each
      // at the beginning of the encrypted package.
      byte[] LenK = new byte[4];
      byte[] LenIV = new byte[4];

      // Consruct the file name for the decrypted file.
      string outFile = System.IO.Path.Combine(_decrFolder, inFile.Substring(0, inFile.LastIndexOf(".")) + ".txt");

      // Use FileStream objects to read the encrypted
      // file (inFs) and save the decrypted file (outFs).
      using (System.IO.FileStream inFs = new System.IO.FileStream(System.IO.Path.Combine(_encrFolder,inFile), System.IO.FileMode.Open))
      {
        inFs.Seek(0, System.IO.SeekOrigin.Begin);
        inFs.Seek(0, System.IO.SeekOrigin.Begin);
        inFs.Read(LenK, 0, 3);
        inFs.Seek(4, System.IO.SeekOrigin.Begin);
        inFs.Read(LenIV, 0, 3);

        // Convert the lengths to integer values.
        int lenK = BitConverter.ToInt32(LenK, 0);
        int lenIV = BitConverter.ToInt32(LenIV, 0);

        // Determine the start postition of
        // the ciphter text (startC)
        // and its length(lenC).
        int startC = lenK + lenIV + 8;
        int lenC = (int)inFs.Length - startC;

        // Create the byte arrays for
        // the encrypted Rijndael key,
        // the IV, and the cipher text.
        byte[] KeyEncrypted = new byte[lenK];
        byte[] IV = new byte[lenIV];

        // Extract the key and IV
        // starting from index 8
        // after the length values.
        inFs.Seek(8, System.IO.SeekOrigin.Begin);
        inFs.Read(KeyEncrypted, 0, lenK);
        inFs.Seek(8 + lenK, System.IO.SeekOrigin.Begin);
        inFs.Read(IV, 0, lenIV);
        System.IO.Directory.CreateDirectory(_decrFolder);
        // Use RSACryptoServiceProvider
        // to decrypt the Rijndael key.
        byte[] KeyDecrypted = _rsa.Decrypt(KeyEncrypted, false);

        // Decrypt the key.
        ICryptoTransform transform = rjndl.CreateDecryptor(KeyDecrypted, IV);

        // Decrypt the cipher text from
        // from the FileSteam of the encrypted
        // file (inFs) into the FileStream
        // for the decrypted file (outFs).
        using (System.IO.FileStream outFs = new System.IO.FileStream(outFile, System.IO.FileMode.Create))
        {
          int count = 0;
          int offset = 0;

          // blockSizeBytes can be any arbitrary size.
          int blockSizeBytes = rjndl.BlockSize / 8;
          byte[] data = new byte[blockSizeBytes];


          // By decrypting a chunk a time,
          // you can save memory and
          // accommodate large files.

          // Start at the beginning
          // of the cipher text.
          inFs.Seek(startC, System.IO.SeekOrigin.Begin);
          using (CryptoStream outStreamDecrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
          {
            do
            {
              count = inFs.Read(data, 0, blockSizeBytes);
              offset += count;
              outStreamDecrypted.Write(data, 0, count);
            } while (count > 0);

            outStreamDecrypted.FlushFinalBlock();
            outStreamDecrypted.Close();
          }
          outFs.Close();
        }
        inFs.Close();
      }
    }
  }
}

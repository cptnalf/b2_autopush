using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace BackupLib
{
  using System.Security.Cryptography;

  using PemReader =Org.BouncyCastle.OpenSsl.PemReader;
  using RsaKeyParameters = Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters;
  using AsymmetricCipherKeyPair = Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair;

  public class KeyLoader
  {
    public static RSA LoadRSAKey(string key) 
    { 
      var keyfile = new FileStream(key, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      var sr = new System.IO.StreamReader(keyfile);
      return LoadRSAKey(sr);
    }

    public static RSA LoadRSAKey(StreamReader sr)
    {
      bool isprivate = false;
      var rdr = new Org.BouncyCastle.OpenSsl.PemReader(sr);
      var o = rdr.ReadObject();
      rdr = null;
      sr.Dispose();
      sr = null;

      var rsaparams = new RSAParameters();

      if (o is Org.BouncyCastle.Crypto.AsymmetricKeyParameter)
        {
          /* only have public key, so populate that. */
          RsaKeyParameters kp = o as RsaKeyParameters;
          
          rsaparams.Exponent=kp.Exponent.ToByteArrayUnsigned();
          rsaparams.Modulus=kp.Modulus.ToByteArrayUnsigned();
        }
      if (o is Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)
        {
          /* have both, so populate the private key. */
          var p = o as Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair;
          var rsap = p.Private as Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters;
          rsaparams.Q=rsap.Q.ToByteArrayUnsigned();
          rsaparams.InverseQ=rsap.QInv.ToByteArrayUnsigned();
          rsaparams.P=rsap.P.ToByteArrayUnsigned();
          rsaparams.Modulus=rsap.Modulus.ToByteArrayUnsigned();
          rsaparams.DQ=rsap.DQ.ToByteArrayUnsigned();
          rsaparams.DP=rsap.DP.ToByteArrayUnsigned();
          rsaparams.D=rsap.Exponent.ToByteArrayUnsigned();
          rsaparams.Exponent=rsap.PublicExponent.ToByteArrayUnsigned();
          isprivate = true;
        }
      
      var rsa = RSA.Create();
      rsa.ImportParameters(rsaparams);

      return rsa;
    }
  }
}

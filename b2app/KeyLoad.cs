using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace b2app
{
  using AsymmetricCipherKeyPair = Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair;
  using RsaKeyParamters = Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters;
  using RsaPrivateParams = Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters;
  
  public class KeyLoad
  {
    public System.Security.Cryptography.AsymmetricAlgorithm loadPubKey(string filename)
    {
      Org.BouncyCastle.OpenSsl.PemReader rdr = new Org.BouncyCastle.OpenSsl.PemReader(new System.IO.StreamReader(new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite)));

      Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair p = null;
      object pemo = null;
      do {
        pemo = rdr.ReadObject();
        if (pemo != null) 
          { 
            p = pemo as AsymmetricCipherKeyPair;
            if (p != null)
              {
                System.Security.Cryptography.RSAParameters rsap = new System.Security.Cryptography.RSAParameters();
                if (p.Private is RsaPrivateParams)
                  {
                    RsaPrivateParams rpp = p.Private as RsaPrivateParams;
                    rsap.P = rpp.P.ToByteArray();
                    rsap.Q = rpp.Q.ToByteArray();
                    rsap.InverseQ = rpp.QInv.ToByteArray();
                    rsap.Exponent = rpp.Exponent.ToByteArray();
                    rsap.DQ = rpp.DQ.ToByteArray();
                    rsap.DP = rpp.DP.ToByteArray();
                    rsap.Modulus = rpp.Modulus.ToByteArray();
                  }

              }

            if (pemo is RsaKeyParamters)
              {
                RsaKeyParamters rsakey = pemo as RsaKeyParamters;
                System.Security.Cryptography.RSAParameters p1 = new System.Security.Cryptography.RSAParameters();
                p1.Exponent = rsakey.Exponent.ToByteArray();
                p1.Modulus = rsakey.Modulus.ToByteArray();

                var rsa = System.Security.Cryptography.RSA.Create();
                rsa.ImportParameters(p1);
                return rsa;
              }
          }
      } while(pemo != null);
      
      return null;
    }
  }
}

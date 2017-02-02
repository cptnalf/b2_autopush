using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestBackupLib
{
  using Microsoft.VisualStudio.TestTools.UnitTesting;

  using Encoding = System.Text.Encoding;
  using System.Security.Cryptography;

  [TestClass]
  public class Encrypting
  {
    protected object _pemRead(string key)
    {
      var keyfile = new MemoryStream();
      _loadStream(keyfile, key);
      var sr = new StreamReader(keyfile);

      var rdr = new Org.BouncyCastle.OpenSsl.PemReader(sr);
      var o = rdr.ReadObject();
      rdr = null;
      sr.Dispose();
      sr = null;
      keyfile.Dispose();
      keyfile = null;

      Assert.IsNotNull(o);

      return o;
    }

    [TestMethod]
    public void LoadPEMPublicKey()
    {
      var o = _pemRead(_PUBKEY);
      
      var p = o as Org.BouncyCastle.Crypto.AsymmetricKeyParameter;
      Assert.IsNotNull(p);
      Assert.IsFalse(p.IsPrivate);
      var rsak = p as Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters;
      Assert.IsNotNull(rsak);
      Assert.IsFalse(rsak.IsPrivate);
    }

    [TestMethod]
    public void TestRSAPubEnc()
    {
      var rsak = _pemRead(_PUBKEY) as Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters;
      Assert.IsNotNull(rsak);

      var rsa = RSA.Create();
      var rsaparams = new RSAParameters 
        { 
        Exponent=rsak.Exponent.ToByteArrayUnsigned()
        , Modulus=rsak.Modulus.ToByteArrayUnsigned()
        };
      rsa.ImportParameters(rsaparams);
      {
        var exp = rsa.ExportParameters(false);
        Assert.AreEqual(rsaparams.Modulus.Length, exp.Modulus.Length);
        CollectionAssert.AreEqual(rsaparams.Modulus, exp.Modulus);
        CollectionAssert.AreEqual(rsaparams.Exponent, exp.Exponent);
      }
      Assert.AreEqual<int>(2048,rsa.KeySize);
      /*
      http://stackoverflow.com/questions/21702662/system-security-cryptography-cryptographicexception-bad-length-in-rsacryptoser
      * sigh. had i known that i can't encrypt a shitton with RSA
      * i might not have tried to do so.
      * the 'data' to the rsa encryption bit needs to be less than 200 bytes?
      */
      var data = Encoding.UTF8.GetBytes(_STRING2ENC);
      var bytes = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
      Assert.IsNotNull(bytes);
      Assert.IsTrue(bytes.Length > 0);

      var b64 = Convert.ToBase64String(bytes);
      //Assert.AreEqual(_ENCSTRING, b64,false);
    }
/*
 *       {
        var fs = new System.IO.FileStream("c:\\tmp\\rsaout.txt", System.IO.FileMode.Create, System.IO.FileAccess.Write );
        var b64 = Convert.ToBase64String(data);
        var b64utfbytes = System.Text.Encoding.UTF8.GetBytes(b64);
        fs.Write(b64utfbytes, 0, b64utfbytes.Length);
        fs.Flush();
        fs.Dispose();
        fs = null;
      }
*/
    
    [TestMethod]
    public void LoadPEMPrivateKey()
    {
      var o = _pemRead(_PRIVATEKEY);

      var p = o as Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair;
      Assert.IsNotNull(p);

      Assert.IsNotNull(p.Private);
      Assert.IsNotNull(p.Public);
      var rsap = p.Private as Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters;
      Assert.IsNotNull(rsap);
    }

    [TestMethod]
    public void TestRSAPrivateDec()
    {
      var p = _pemRead(_PRIVATEKEY) as Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair;
      var rsap = p.Private as Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters;
      Assert.IsNotNull(rsap);

      var rsa = RSA.Create();
      {
        var rsaparams = new RSAParameters
          {
             Q=rsap.Q.ToByteArrayUnsigned()
            ,InverseQ=rsap.QInv.ToByteArrayUnsigned()
            , P=rsap.P.ToByteArrayUnsigned()
            , Modulus=rsap.Modulus.ToByteArrayUnsigned()
            , DQ=rsap.DQ.ToByteArrayUnsigned()
            , DP=rsap.DP.ToByteArrayUnsigned()
            , D=rsap.Exponent.ToByteArrayUnsigned()
            , Exponent=rsap.PublicExponent.ToByteArrayUnsigned()
          };
        rsa.ImportParameters(rsaparams);
      }
      {
        var data = Convert.FromBase64String(_ENCSTRING);
        var decdata = rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA1);
        var str = System.Text.Encoding.UTF8.GetString(decdata);

        Assert.AreEqual(_STRING2ENC, str, false);
      }
    }

    [TestMethod]
    public void TestKeyLoader()
    {
      var keystrm = new MemoryStream();
      _loadStream(keystrm,_PUBKEY);
      var pubstrm = new StreamReader(keystrm);
      var rsapub = BackupLib.KeyLoader.LoadRSAKey(pubstrm);

      var data = Encoding.UTF8.GetBytes(_STRING2ENC);
      var bytes = rsapub.Encrypt(data, RSAEncryptionPadding.OaepSHA1);

      Assert.IsNotNull(bytes);
      Assert.IsTrue(bytes.Length > 0);

      keystrm = new MemoryStream();
      _loadStream(keystrm, _PRIVATEKEY);
      var pvtstrm = new StreamReader(keystrm);
      var rsapvt = BackupLib.KeyLoader.LoadRSAKey(pvtstrm);

      data = rsapvt.Decrypt(bytes, RSAEncryptionPadding.OaepSHA1);
      var str = Encoding.UTF8.GetString(data);
      Assert.AreEqual(_STRING2ENC, str);
    }

    [TestMethod]
    public void TestAESEnc()
    {
      var aes = Aes.Create();

      var keybytes = Convert.FromBase64String(_AESKEY);

      aes.Mode = CipherMode.CBC;
      aes.Padding = PaddingMode.PKCS7;
      aes.KeySize = keybytes.Length * 8;

      aes.Key = keybytes;
      aes.IV = Convert.FromBase64String(_AESIV);

      var srcBytes = Encoding.UTF8.GetBytes(_PUBKEY);
      var instrm = new MemoryStream(srcBytes);
      var deststrm = new MemoryStream();

      var xform = aes.CreateEncryptor();
      var cs = new CryptoStream(deststrm, xform, CryptoStreamMode.Write);
      instrm.CopyTo(cs);
      cs.FlushFinalBlock();
      cs.Flush();
      var encbytes = deststrm.ToArray();
      cs.Dispose();
      cs = null;

      Assert.IsNotNull(encbytes);
      Assert.IsTrue(encbytes.Length > 0);

      var newarr = Convert.ToBase64String(encbytes);
      Assert.AreEqual(_AESENC, newarr);
    }

    [TestMethod]
    public void TestAESDec()
    {
      var instrm = new MemoryStream(Convert.FromBase64String(_AESENC));
      var aes = Aes.Create();
      
      aes.Mode = CipherMode.CBC;
      aes.Padding = PaddingMode.PKCS7;
      {
        var keybytes = Convert.FromBase64String(_AESKEY);
        aes.KeySize = keybytes.Length * 8;
        aes.Key = keybytes;
      }
      aes.IV = Convert.FromBase64String(_AESIV);

      var deststrm = new MemoryStream();
      var xform = aes.CreateDecryptor();
      var cs = new CryptoStream(instrm, xform, CryptoStreamMode.Read);
      cs.CopyTo(deststrm);
      cs.Dispose();
      cs = null;

      Assert.IsTrue(deststrm.Length > 0);

      var str = Encoding.UTF8.GetString(deststrm.ToArray());
      Assert.AreEqual(_PUBKEY, str);
    }

    private void _loadStream(Stream strm, string str)
    {
      var wr = new StreamWriter(strm, Encoding.UTF8,1024,true);
      wr.Write(str);
      wr.Dispose();
      strm.Seek(0, SeekOrigin.Begin);
    }

    const string _AESKEY = "pTapq8ksL5MufY/9m7PEaA==";
    const string _AESIV = "+jfy5J4gt9LGjY38JKkJGA==";
    const string _AESENC = "i//+pi/BdRBqE8ME0ombHirpLqfVx9VoRidGdjp6Hqv9vgZTpw10ynvMqTj4zM6l9iHVim3kg+IKYxwTG3uLSDK5fHTZDufi79FkSjdUZhk/vRnHvLnnDX0jtp/v3geAsRHRBbafrQwyb+VDay4ceuO6si9FlBt79GbWtni/YjHtUwsy9BUQFUIqsU7Le6yxv++T3MN5dlBxefsGoAjSxtBOKvbwBIjLJMcRfIniMLRSrY26vzIbecLnJRWy/qMBdRuzdXog5y0LDmUhvN/hx2AKOSlITfrBZ3bbIP87gVooLnElE0FFlXReTACC9teqBVGexkqewCFyOYA8bGLKHpvwwEHIaZ21p6bTOrU+74TDFZSDFnCTS7xyFH5chLc8tJI4GLI/HsgVofIOIYv1Mdsqua+51ai8syutVUFasQ9WJjJRkAX/YfWjdoCMFT2fWLLUgwQleMFWoIcRXQwuLgbHucccSE/CGpqNDEpx+RHTuPb07KASYJsDOGdFKEfdpfOwdU675QeQxn1FuMVms072sYsDMWo+0Q5g7M19RiWojXD+vdVrL4jbKDhxOPxAEKm4NkD4lGxSjlX+BgHf1vo7SOwl1upZo3fAiZ4x3Lg=";
    
    const string _STRING2ENC = "blargity blarg";
    /* base 64 rsa encrypted string. */
    const string _ENCSTRING = @"IkVZZjre7K2AL97ptzJNd724I0cthbMXkesrob2apS+N4l6U6dW0P/HjeExhdAZ6WY3uqrZ/pAYG1AEYk1b5IhvaBwCi4R7LugRHsDQVWFXwGpmypEGT2Jc2c7qWsPS0xzvfo2v0JDwDvYIXQvI8p/jBq9kynbU8DFq+Kb1tO7I7++aSV7jXfaophgWMcWX5AW51racSkUe4Wze5wFG79EK/friOaH0fCqNrjzvev+eWlGiGD5DJ62qNPkX5rNEiT94um9BEBOKwnZDkGxNs7CJcu3xlZp5eUgHNNGViRQXCByOb6/t814Mh1MD1QabLS/GIF20DNNvnMuDBRXHK4A==";

    public const string _PUBKEY =
            @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEApKGUIH1IfgyoSirpwg7j
eVpdYwcT5/WzIVQazlm7ZqcdRrHtSM2VZpz2YRNVCgSJnU5hlbw6ICHlsRSYkrJ4
gdPLMm7+QLt816KgYFvAWjCu7VafSmn4eSnu+MObwM7xCbUI1PARqENZG2oa2jGZ
GgYqd2+AuOrmQjf4GVp7f1fDRZTiY1jGAdWF++9lOXKOTFmzuy6DPHu+JJUayEkX
aQL0XaywmsowvWvPpbInSaqGqH20wnZWUVZoTLq4KDUzESoR/z+ZA3WFdZ4oTj6Z
XS0ZSlG8jL3vT5dkT9fxd7nTBLvGg3IjGtQhSVtk5xBrAosUa56UDg1flo2asbB8
9QIDAQAB
-----END PUBLIC KEY-----

";
    public const string _PRIVATEKEY =
@"-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEApKGUIH1IfgyoSirpwg7jeVpdYwcT5/WzIVQazlm7ZqcdRrHt
SM2VZpz2YRNVCgSJnU5hlbw6ICHlsRSYkrJ4gdPLMm7+QLt816KgYFvAWjCu7Vaf
Smn4eSnu+MObwM7xCbUI1PARqENZG2oa2jGZGgYqd2+AuOrmQjf4GVp7f1fDRZTi
Y1jGAdWF++9lOXKOTFmzuy6DPHu+JJUayEkXaQL0XaywmsowvWvPpbInSaqGqH20
wnZWUVZoTLq4KDUzESoR/z+ZA3WFdZ4oTj6ZXS0ZSlG8jL3vT5dkT9fxd7nTBLvG
g3IjGtQhSVtk5xBrAosUa56UDg1flo2asbB89QIDAQABAoIBAQCEV2tnDq9WvMAQ
Fx0gpa1Q4UaPE6J55jZghWajGNkf9RkAuolf6/u8qFMayFqlGe6yKM8jelNTf0xQ
pJjd3GApJWOEIFt9F/qMsauwqjEfj2EfY3HbdQKMDByRl1U+klyLjB8UZgQbukAI
XKxHWHWVyP0cU+MrQ5FkC/ACGY2LWGxhzdLViF68h3yyQY0QrIX733FHjfVkuhrX
ETnnSpVRr5zDv8N0krpgx5N5cR6SPoFtjTEnTz4KggFKBZg+/g8SBD0qPxC6FRQu
Frc3bVTny1RSPlnvPQ8PLzjnYzat6jPIWe1JilX7Q5B2H3VpVC1sqhNUWcHmIQWn
cpc3MmQlAoGBANQMEIE+i3bXHmy8MUfM/zR9v0m5gD2ib4Q5A/D1ctiiXuRCJqou
l/jfvPYyRHunLvoIJxVLIWvRBHLECfhWUCdTfkn5J3jAMPcxreG3WbZ1fKh1p/1y
TgIw7ZCvaRULaihRJrst7pwQ5UO3jtrkxsZ6fs25m9u7hKYSxyqEsWOLAoGBAMbB
dqJoGgVdcdpqTbqEbtkS5J52U3FnkoELUmp+K/la+Ep/2Fg9uKyJ+4vvnkhH2PNb
Aiaun4rvf+eCkD/TB8FUr3Q8+7WRQ7zzx+YTAuU94aoW4Hh04yq1qc1MBvrHOTkv
yT5+raeZBVTHOhLiqSmHqp/ovqy4FAkfIQ4fN7F/AoGAAXI/npINo3beJ0G1WFcG
mpYM+vS/8iusdQtqgnc6HE4nNYlZ+CkvMixcfpVjMDC4uk3Z7mQ/yxt2202I/9+e
1lXUc662XTV6YAU/uV1lyD/O5NtAlRL1g0BQLn9zyQf15mZ/TCGJEhlvZuHWoJmU
3X+yY7bTYFFMG1Hfd+PFzfcCgYEApQzNTqqySRhDTsSOTcBiKMOGtIzAWGFRCPZ0
91hVfhnsLDmkWArRS/69tIRE5fM8F0LRM3w5ou+mQINs9INzYjnIBfgKcsnx/XxX
2Riag/HybwPWXlF6v+Hh40kqVqCQRYwIS2x5Gr947OEQudQd9A3kRCzMArROdxCx
q3+DCVMCgYB1upXfZly1r8IrUH+A0mDZWNphe9TzWhICj9GtbzGDwYfIGEK84+u0
sX0hk2wxzaw8ca/sHa6UHHvwRnac8n7wf74l+d/tfaoksn8eFZISn5Ml2ydiEkQq
n+4uzRUs5vE1bgFv/Puvdj1H8Lqlo3OoIw/2vDrpzFkA8hJb9ELUsw==
-----END RSA PRIVATE KEY-----";

    }
}

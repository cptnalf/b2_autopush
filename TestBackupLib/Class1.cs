using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestBackupLib
{
  using Microsoft.VisualStudio.TestTools.UnitTesting;
  using Encoding = System.Text.Encoding;
  using System.Security.Cryptography;

  [TestClass]
  public class Class1
  {
    public TestContext tc {get;set; }

    public Class1()
    {
    }

    [TestMethod]
    public void TestLocalLister()
    {
      var ll = new BackupLib.LocalLister();
      var fl = ll.getList("c:\\tmp");

      Assert.IsNotNull(fl);
      Assert.IsTrue(fl.Count > 0);
    }

    [TestMethod]
    public void TestDirDiff()
    {
      var ll = new BackupLib.LocalLister();
      var fl = ll.getList("c:\\tmp");
      
      var dd = new BackupLib.DirDiff();
      var cmpres = dd.compare(fl, fl);
      Assert.IsNotNull(cmpres);
      Assert.IsTrue(cmpres.Count == 0);
    }

    [TestMethod]
    public void TestDirDiffD()
    {
      var ll = new BackupLib.LocalLister();
      var fl = ll.getList("c:\\tmp");
      
      var fl2 = new List<BackupLib.FreezeFile>(fl);
      fl2.Add(new BackupLib.FreezeFile { path="blarg/foo.jpg" });

      var dd = new BackupLib.DirDiff();
      var cmpres = dd.compare(fl, fl2);
      Assert.IsNotNull(cmpres);
      Assert.IsTrue(cmpres.Count > 0);

      var x = cmpres.FirstOrDefault();
      Assert.IsNotNull(x);
    }

    [TestMethod]
    public void LoadPEMPublicKey()
    {
      var keyfile = new System.IO.MemoryStream();
      _loadStream(keyfile, _PUBKEY);
      var sr = new System.IO.StreamReader(keyfile);

      var rdr = new Org.BouncyCastle.OpenSsl.PemReader(sr);
      var o = rdr.ReadObject();
      rdr = null;
      sr.Dispose();
      sr = null;
      keyfile.Dispose();
      keyfile = null;

      Assert.IsNotNull(o);

      var p = o as Org.BouncyCastle.Crypto.AsymmetricKeyParameter;
      Assert.IsNotNull(p);
      Assert.IsFalse(p.IsPrivate);
      var rsak = p as Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters;
      Assert.IsNotNull(rsak);
      Assert.IsFalse(rsak.IsPrivate);

      var rsa = RSA.Create();
      var rsaparams = new RSAParameters 
        { 
        Exponent=rsak.Exponent.ToByteArray()
        , Modulus=rsak.Modulus.ToByteArray()
        };
      rsa.ImportParameters(rsaparams);
      var data = Encoding.UTF8.GetBytes("blargity blarg");
      var bytes = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
      Assert.IsNotNull(bytes);
      Assert.IsTrue(bytes.Length > 0);
    }
    
    [TestMethod]
    public void LoadPEMPrivateKey()
    {
      var keyfile = new System.IO.MemoryStream();
      _loadStream(keyfile, _PRIVATEKEY);
      var sr = new System.IO.StreamReader(keyfile);

      var rdr = new Org.BouncyCastle.OpenSsl.PemReader(sr);
      var o = rdr.ReadObject();
      rdr = null;
      sr.Dispose();
      sr = null;
      keyfile.Dispose();
      keyfile = null;

      Assert.IsNotNull(o);

      var p = o as Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair;
      Assert.IsNotNull(p);

      Assert.IsNotNull(p.Private);
      Assert.IsNotNull(p.Public);
      var rsap = p.Private as Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters;
      Assert.IsNotNull(rsap);
    }

    private void _loadStream(System.IO.Stream strm, string str)
    {
      var wr = new System.IO.StreamWriter(strm, System.Text.Encoding.UTF8,1024,true);
      wr.Write(str);
      wr.Dispose();
      strm.Seek(0, System.IO.SeekOrigin.Begin);
    }

    const string _PUBKEY =
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
    const string _PRIVATEKEY =
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

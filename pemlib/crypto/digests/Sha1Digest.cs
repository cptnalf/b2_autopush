using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Org.BouncyCastle.Crypto.Digests
{
  using System.Security.Cryptography;

  public class Sha1Digest : IDigest
  { 
    private IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);

    public string AlgorithmName {get { return "Sha1"; } }

    public void BlockUpdate(byte[] input, int inOff, int length) { _hash.AppendData(input, inOff, length); }

    public int DoFinal(byte[] output, int outOff)
    {
      byte[] b = _hash.GetHashAndReset();
      for(int i=0; i <b.Length; ++i )
        { output[outOff + i] = b[i]; }

      return 0;
    }

    public int GetByteLength()
    {
      return 99;
    }

    public int GetDigestSize() { return SHA1.Create().HashSize; }
    public void Reset() { _hash.GetHashAndReset(); }

    public void Update(byte input) { _hash.AppendData(new byte[] { input}); }
  }
}

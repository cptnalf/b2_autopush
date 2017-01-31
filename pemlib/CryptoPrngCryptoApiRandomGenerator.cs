using System;
using System.Security.Cryptography;

namespace Org.BouncyCastle.Crypto.Prng
{
	/// <remarks>Generic interface for objects generating random bytes.</remarks>
	public interface IRandomGenerator
	{
		/// <summary>Add more seed material to the generator.</summary>
		/// <param name="seed">A byte array to be mixed into the generator's state.</param>
		void AddSeedMaterial(byte[] seed);

		/// <summary>Add more seed material to the generator.</summary>
		/// <param name="seed">A long value to be mixed into the generator's state.</param>
		void AddSeedMaterial(long seed);

		/// <summary>Fill byte array with random values.</summary>
		/// <param name="bytes">Array to be filled.</param>
		void NextBytes(byte[] bytes);

		/// <summary>Fill byte array with random values.</summary>
		/// <param name="bytes">Array to receive bytes.</param>
		/// <param name="start">Index to start filling at.</param>
		/// <param name="len">Length of segment to fill.</param>
		void NextBytes(byte[] bytes, int start, int len);
	}

    /// <summary>
    /// Uses Microsoft's RNGCryptoServiceProvider
    /// </summary>
    public class CryptoApiRandomGenerator : IRandomGenerator
    {
        private readonly RandomNumberGenerator rndProv;

        public CryptoApiRandomGenerator() : this(RandomNumberGenerator.Create()) { }

        public CryptoApiRandomGenerator(RandomNumberGenerator rng) { this.rndProv = rng; }

#region IRandomGenerator Members

        public virtual void AddSeedMaterial(byte[] seed)
        {
            // We don't care about the seed
        }

        public virtual void AddSeedMaterial(long seed)
        {
            // We don't care about the seed
        }

        public virtual void NextBytes(byte[] bytes)
        { rndProv.GetBytes(bytes); }

        public virtual void NextBytes(byte[] bytes, int start, int len)
        {
            if (start < 0)
                throw new ArgumentException("Start offset cannot be negative", "start");
            if (bytes.Length < (start + len))
                throw new ArgumentException("Byte array too small for requested offset and length");

            if (bytes.Length == len && start == 0) 
            {
                NextBytes(bytes);
            }
            else 
            {
                byte[] tmpBuf = new byte[len];
                NextBytes(tmpBuf);
                Array.Copy(tmpBuf, 0, bytes, start, len);
            }
        }

#endregion
    }
}

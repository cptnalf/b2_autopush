using System;
using System.Collections;

namespace Org.BouncyCastle.Utilities.IO.Pem
{
  public interface PemObjectGenerator
	{
		/// <returns>
		/// A <see cref="PemObject"/>
		/// </returns>
		/// <exception cref="PemGenerationException"></exception>
		PemObject Generate();
	}

	public class PemObject : PemObjectGenerator
	{
		private string		type;
		private IList		headers;
		private byte[]		content;

		public PemObject(string type, byte[] content)
			: this(type, Platform.CreateArrayList(), content)
		{
		}

		public PemObject(String type, IList headers, byte[] content)
		{
			this.type = type;
            this.headers = Platform.CreateArrayList(headers);
			this.content = content;
		}

		public string Type { get { return type; } }
		public IList Headers { get { return headers; } }
		public byte[] Content { get { return content; } }
		public PemObject Generate() { return this; }
	}
}
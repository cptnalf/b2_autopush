using System;

namespace Org.BouncyCastle.Asn1
{
  using IOException = System.IO.IOException;

  public class Asn1Exception : IOException
	{
		public Asn1Exception() : base() { }

		public Asn1Exception( string message) : base(message) { }
    public Asn1Exception( string message, Exception exception) : base(message, exception) { }
	}
}

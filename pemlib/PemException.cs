using System;
using System.IO;

namespace Org.BouncyCastle.OpenSsl
{
  public class PemException: IOException
	{
		public PemException(string message) : base(message) { }
		public PemException(string message, Exception exception) : base(message, exception) { }
	}
}

namespace Org.BouncyCastle.Security
{
    public class PasswordException: IOException
	{
		public PasswordException(string message) : base(message) { }
		public PasswordException(string message, Exception exception) : base(message, exception) { }
	}
}
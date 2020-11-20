using System;

namespace BUCommon
{
  public class CheckOptionException : ApplicationException
  {
    public CheckOptionException() : base() {}
    public CheckOptionException(string msg) : base(msg) { }
    public CheckOptionException(string msg, Exception exp) : base(msg, exp) {}
  }
}

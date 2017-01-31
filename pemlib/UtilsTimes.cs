namespace Org.BouncyCastle.Utilities
{
  using ArgumentException = System.ArgumentException;
  using System.Reflection;

  internal abstract class Enums
  {
    internal static T GetEnumValue<T>(string s)
    {
      if (!typeof(T).GetTypeInfo().IsEnum) { throw new ArgumentException("Must be an enum"); }

      if (string.IsNullOrWhiteSpace(s) || s.IndexOf(',') >= 0 || !char.IsLetter(s[0]) )
        { throw new ArgumentException("enum const is not well formed"); }

      s = s.Replace('-', '_');
      s = s.Replace('/', '_');
      return (T)System.Enum.Parse(typeof(T), s);
    }
  }

    public sealed class Times
    {
        private static long NanosecondsPerTick = 100L;

        public static long NanoTime()
        {
            return System.DateTime.UtcNow.Ticks * NanosecondsPerTick;
        }
    }
}
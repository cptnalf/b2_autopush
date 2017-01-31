namespace Org.BouncyCastle.Utilities.Collections
{
  using System.Collections;

  using StringBuilder = System.Text.StringBuilder;

  public abstract class CollectionUtilities
  {
    public static string ToString(IEnumerable c)
    {
      StringBuilder sb = new StringBuilder("[");

      IEnumerator e = c.GetEnumerator();

      if (e.MoveNext())
        {
          sb.Append(e.Current.ToString());

          while (e.MoveNext())
            {
              sb.Append(", ");
              sb.Append(e.Current.ToString());
            }
        }
      sb.Append(']');

      return sb.ToString();
    }
  }
}
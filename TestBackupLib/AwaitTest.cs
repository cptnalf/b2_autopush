using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBackupLib
{
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
  using Assert = NUnit.Framework.Assert;

  /// <summary>
  /// will probably need to construct an upload server that dumps files to 
  /// test this with any degree of usefulness.
  /// </summary>
  [TestClass]
  public class AwaitTest
  {
    private async Task<int> _randomWait(string filename, Random r)
    {
      int x = 0;
      lock(r) { x = r.Next(60000); }
      await Task.Delay(x);

      return x;
    }
    
    private async Task<int> _totalTime()
    {
      string[] files = new string[] { "blarg", "foo.jpg", "baz.jpg", "flarg.jpg" };
      Random r = new Random();

      var parts = files.Select(f => _randomWait(f, r));
      int[] ints = await Task.WhenAll(parts);

      return ints.Sum();
    }

    [TestMethod]
    public void TestAwaitAll()
    {
      var res = _totalTime();
      res.Wait();
      Assert.That( res.Result > 0);
    }
  }
}

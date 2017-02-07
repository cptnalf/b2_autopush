using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUCommon
{
  public class FileSvcBase
  {
    public static string[] ParseConnStr(string str)
    {
      /* blarg=bloog; flarg = flug ;
       * blarg="bla=;rg";
       * 
       * start with just foo:blarg
       */
      
      if (string.IsNullOrWhiteSpace(str)) { throw new ArgumentNullException("connStr"); }

      var parts = str.Split(':');
      return parts;
    }
  }
}

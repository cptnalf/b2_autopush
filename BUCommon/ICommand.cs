using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUCommon
{
  public interface ICommand
  {
    string helptext {get;}
    void run();
  }
}

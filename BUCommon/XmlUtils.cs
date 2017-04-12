using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BUCommon
{
  using System.IO;
  using System.Xml.Serialization;

  public class XmlUtils
  {
    public static void WriteXml<T>(string filedest, T obj, Type[] subTypes)
    {
      var strm = new FileStream(filedest, FileMode.Create, FileAccess.Write, FileShare.Read);

      var xdr = _SerializerMake(typeof(T), subTypes);
      xdr.Serialize(strm, obj);
      strm.Close();
      strm.Dispose();
      strm = null;
      xdr = null;
    }

    public static T ReadXml<T>(string filesrc, Type[] subTypes) where T : class
    {
      if (!File.Exists(filesrc)) { return null; }
      var strm = new FileStream(filesrc, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      var xdr  = _SerializerMake(typeof(T), subTypes);
      var ucx = xdr.Deserialize(strm) as T;
      xdr = null;
      strm.Close();
      strm.Dispose();
      strm = null;

      return ucx;
    }

    private static XmlSerializer _SerializerMake(Type main, Type[] subTypes) { return new XmlSerializer(main, subTypes); }
  }
}

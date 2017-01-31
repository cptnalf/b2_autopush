using System;
using System.Collections;
using System.Collections.Generic;

using Org.BouncyCastle.Utilities;

namespace Org.BouncyCastle.Asn1
{
    public class Asn1EncodableVector : IEnumerable<Asn1Encodable>
    {
      private IList<Asn1Encodable> v = new List<Asn1Encodable>();

		public static Asn1EncodableVector FromEnumerable( IEnumerable<Asn1Encodable> e)
		{
			Asn1EncodableVector v = new Asn1EncodableVector();
			foreach (Asn1Encodable obj in e) { v.Add(obj); }
			return v;
		}

		public Asn1EncodableVector( params Asn1Encodable[] v) { Add(v); }

		public void Add( params Asn1Encodable[] objs) 
    { foreach (Asn1Encodable obj in objs) { v.Add(obj); } }

		public void AddOptional( params Asn1Encodable[] objs)
		{
			if (objs != null)
			  { foreach (Asn1Encodable obj in objs) { if (obj != null) { v.Add(obj); } } }
		}

		public Asn1Encodable this[ int index] { get { return v[index]; } }

		public int Count { get { return v.Count; } }

		public IEnumerator<Asn1Encodable> GetEnumerator() { return v.GetEnumerator(); }

    IEnumerator IEnumerable.GetEnumerator() { return v.GetEnumerator(); }
  }
}
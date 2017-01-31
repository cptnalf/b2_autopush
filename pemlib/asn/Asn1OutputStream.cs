using System;
using System.IO;

namespace Org.BouncyCastle.Asn1
{
    public class Asn1OutputStream : DerOutputStream
    { public Asn1OutputStream(Stream os) : base(os) { } }

    // TODO Make Obsolete in favour of Asn1OutputStream?
    public class BerOutputStream : DerOutputStream
    { public BerOutputStream(Stream os) : base(os) { } }
}
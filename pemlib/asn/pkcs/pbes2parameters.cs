using System;

namespace Org.BouncyCastle.Asn1.Pkcs
{
    public class PbeS2Parameters
        : Asn1Encodable
    {
        private readonly KeyDerivationFunc func;
        private readonly EncryptionScheme scheme;

        public static PbeS2Parameters GetInstance(object obj)
        {
            if (obj == null)
                return null;
            PbeS2Parameters existing = obj as PbeS2Parameters;
            if (existing != null)
                return existing;
            return new PbeS2Parameters(Asn1Sequence.GetInstance(obj));
        }

        public PbeS2Parameters(KeyDerivationFunc keyDevFunc, EncryptionScheme encScheme)
        {
            this.func = keyDevFunc;
            this.scheme = encScheme;
        }

        public KeyDerivationFunc KeyDerivationFunc
        {
            get { return func; }
        }

        public EncryptionScheme EncryptionScheme
        {
            get { return scheme; }
        }

        public override Asn1Object ToAsn1Object()
        {
            return new DerSequence(func, scheme);
        }
    }
}
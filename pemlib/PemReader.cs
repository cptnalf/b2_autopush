using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Utilities.IO.Pem;

#if ENABLE_EC
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.TeleTrust;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Pkcs;
#endif

namespace Org.BouncyCastle.OpenSsl
{
  public interface IPasswordFinder
  { char[] GetPassword(); }

  /**
  * Class for reading OpenSSL PEM encoded streams containing 
  * X509 certificates, PKCS8 encoded keys and PKCS7 objects.
  * <p>
  * In the case of PKCS7 objects the reader will return a CMS ContentInfo object. Keys and
  * Certificates will be returned using the appropriate java.security type.</p>
  */
  public class PemReader : Org.BouncyCastle.Utilities.IO.Pem.PemReader
  {
    private readonly IPasswordFinder pFinder;

    /**
    * Create a new PemReader
    *
    * @param reader the Reader
    */
    public PemReader( TextReader reader) : this(reader, null) { }

        /**
        * Create a new PemReader with a password finder
        *
        * @param reader the Reader
        * @param pFinder the password finder
        */
        public PemReader(TextReader reader, IPasswordFinder	pFinder) : base(reader) { this.pFinder = pFinder; }

        public object ReadObject()
        {
            PemObject obj = ReadPemObject();

            if (obj == null) { return null; }


            if (Platform.EndsWith(obj.Type, "PRIVATE KEY"))
                return ReadPrivateKey(obj);

            switch (obj.Type)
            {
                case "PUBLIC KEY":
                    return ReadPublicKey(obj);
                case "RSA PUBLIC KEY":
                    return ReadRsaPublicKey(obj);
                // TODO Add back in when tests done, and return type issue resolved
                //case "EC PARAMETERS":
                //	return ReadECParameters(obj);
                default:
                    throw new IOException("unrecognised object: " + obj.Type);
            }
        }

        private AsymmetricKeyParameter ReadRsaPublicKey(PemObject pemObject)
        {
            RsaPublicKeyStructure rsaPubStructure = RsaPublicKeyStructure.GetInstance(
                Asn1Object.FromByteArray(pemObject.Content));

            return new RsaKeyParameters(
                false, // not private
                rsaPubStructure.Modulus, 
                rsaPubStructure.PublicExponent);
        }

        private AsymmetricKeyParameter ReadPublicKey(PemObject pemObject)
        {
            return PublicKeyFactory.CreateKey(pemObject.Content);
        }

        /**
        * Read a Key Pair
        */
        private object ReadPrivateKey(PemObject pemObject)
        {
          //
          // extract the key
          //
          Debug.Assert(Platform.EndsWith(pemObject.Type, "PRIVATE KEY"));

          string type = pemObject.Type.Substring(0, pemObject.Type.Length - "PRIVATE KEY".Length).Trim();
          byte[] keyBytes = pemObject.Content;

          IDictionary<string,string> fields = new Dictionary<string,string>();
          foreach (PemHeader header in pemObject.Headers) { fields[header.Name] = header.Value; }

          if (fields.ContainsKey("Proc-Type"))
            {
              string procType = (string) fields["Proc-Type"];

              if (procType == "4,ENCRYPTED")
                {
                  if (pFinder == null) { throw new PasswordException("No password finder specified, but a password is required"); }

                  char[] password = pFinder.GetPassword();

                  if (password == null) { throw new PasswordException("Password is null, but a password is required"); }

                  string dekInfo = (string) fields["DEK-Info"];
                  string[] tknz = dekInfo.Split(',');

                  string dekAlgName = tknz[0].Trim();
                  byte[] iv = Hex.Decode(tknz[1].Trim());

                  throw new NotImplementedException("Have not pulled in the entire crypto library.");
                  /*
                  keyBytes = PemUtilities.Crypt(false, keyBytes, password, dekAlgName, iv);
                  */
                }
            }

            try
            {
                AsymmetricKeyParameter pubSpec, privSpec;
                Asn1Sequence seq = Asn1Sequence.GetInstance(keyBytes);

                switch (type)
                {
                    case "RSA":
                    {
                        if (seq.Count != 9)
                            throw new PemException("malformed sequence in RSA private key");

                        RsaPrivateKeyStructure rsa = RsaPrivateKeyStructure.GetInstance(seq);

                        pubSpec = new RsaKeyParameters(false, rsa.Modulus, rsa.PublicExponent);
                        privSpec = new RsaPrivateCrtKeyParameters(
                            rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent,
                            rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2,
                            rsa.Coefficient);

                        break;
                    }

                    case "DSA":
                    {
                        if (seq.Count != 6)
                            throw new PemException("malformed sequence in DSA private key");

                        // TODO Create an ASN1 object somewhere for this?
                        //DerInteger v = (DerInteger)seq[0];
                        DerInteger p = (DerInteger)seq[1];
                        DerInteger q = (DerInteger)seq[2];
                        DerInteger g = (DerInteger)seq[3];
                        DerInteger y = (DerInteger)seq[4];
                        DerInteger x = (DerInteger)seq[5];

                        DsaParameters parameters = new DsaParameters(p.Value, q.Value, g.Value);

                        privSpec = new DsaPrivateKeyParameters(x.Value, parameters);
                        pubSpec = new DsaPublicKeyParameters(y.Value, parameters);

                        break;
                    }
#if ENABLE_EC
                    case "EC":
                    {
                        ECPrivateKeyStructure pKey = ECPrivateKeyStructure.GetInstance(seq);
                        AlgorithmIdentifier algId = new AlgorithmIdentifier(
                            X9ObjectIdentifiers.IdECPublicKey, pKey.GetParameters());

                        PrivateKeyInfo privInfo = new PrivateKeyInfo(algId, pKey.ToAsn1Object());

                        // TODO Are the keys returned here ECDSA, as Java version forces?
                        privSpec = PrivateKeyFactory.CreateKey(privInfo);

                        DerBitString pubKey = pKey.GetPublicKey();
                        if (pubKey != null)
                        {
                            SubjectPublicKeyInfo pubInfo = new SubjectPublicKeyInfo(algId, pubKey.GetBytes());

                            // TODO Are the keys returned here ECDSA, as Java version forces?
                            pubSpec = PublicKeyFactory.CreateKey(pubInfo);
                        }
                        else
                        {
                            pubSpec = ECKeyPairGenerator.GetCorrespondingPublicKey(
                                (ECPrivateKeyParameters)privSpec);
                        }

                        break;
                    }
#endif
                    case "ENCRYPTED":
                    {
                        char[] password = pFinder.GetPassword();

                        if (password == null)
                            throw new PasswordException("Password is null, but a password is required");

                        throw new NotImplementedException("decrypting encrypted keys is not currently supported");
                        //return PrivateKeyFactory.DecryptKey(password, EncryptedPrivateKeyInfo.GetInstance(seq));
                    }

                    case "":
                    {
                        return PrivateKeyFactory.CreateKey(PrivateKeyInfo.GetInstance(seq));
                    }

                    default:
                        throw new ArgumentException("Unknown key type: " + type, "type");
                }

                return new AsymmetricCipherKeyPair(pubSpec, privSpec);
            }
            catch (IOException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new PemException(
                    "problem creating " + type + " private key: " + e.ToString());
            }
        }
    }
}
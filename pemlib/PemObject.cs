using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PEMReader
{
  using System.Security.Cryptography;
  using StringBuilder = System.Text.StringBuilder;
  using IOException = System.IO.IOException;
  using CompareOptions = System.Globalization.CompareOptions;
  using CultureInfo = System.Globalization.CultureInfo;

  public class PemObject
  {
    public PemObject()
    {
    }
  }

  public class PemHeader
  {
  }

  public class PemReader
  {
    private const string BeginString = "-----BEGIN ";
		private const string EndString = "-----END ";

    private System.IO.TextReader _reader;

    public PemObject Read()
    {
      PemObject obj = ReadPemObject();

      if (obj == null) { return null; }

// TODO Follow Java build and map to parser objects?
//			if (parsers.Contains(obj.Type))
//				return ((PemObjectParser)parsers[obj.Type]).ParseObject(obj);

      if (CultureInfo.InvariantCulture.CompareInfo.IsSuffix(obj.Type, "PRIVATE KEY", CompareOptions.Ordinal)) 
        { return ReadPrivateKey(obj); }

      switch (obj.Type)
      {
        case "PUBLIC KEY":
            return ReadPublicKey(obj);
        case "RSA PUBLIC KEY":
            return ReadRsaPublicKey(obj);
        case "CERTIFICATE REQUEST":
        case "NEW CERTIFICATE REQUEST":
            return ReadCertificateRequest(obj);
        case "CERTIFICATE":
        case "X509 CERTIFICATE":
            return ReadCertificate(obj);
        case "PKCS7":
        case "CMS":
            return ReadPkcs7(obj);
        case "X509 CRL":
            return ReadCrl(obj);
        case "ATTRIBUTE CERTIFICATE":
            return ReadAttributeCertificate(obj);
// TODO Add back in when tests done, and return type issue resolved
//case "EC PARAMETERS":
//	return ReadECParameters(obj);
        default:
            throw new IOException("unrecognised object: " + obj.Type);
      }
    }

    /// <returns>
		/// A <see cref="PemObject"/>
		/// </returns>
		/// <exception cref="IOException"></exception>
		public PemObject ReadPemObject()
		{
			string line = _reader.ReadLine();

      if (line != null && CultureInfo.InvariantCulture.CompareInfo.IsPrefix(line, BeginString, CompareOptions.Ordinal))
			{
				line = line.Substring(BeginString.Length);
				int index = line.IndexOf('-');
				string type = line.Substring(0, index);

				if (index > 0)
					return LoadObject(type);
			}

			return null;
		}

    private PemObject LoadObject(string type)
		{
			string endMarker = Environment.NewLine + type;
			IList headers = Platform.CreateArrayList();
			StringBuilder buf = new StringBuilder();

			string line;
			while ((line = _reader.ReadLine()) != null
                && CultureInfo.InvariantCulture.CompareInfo.IndexOf(line, endMarker, CompareOptions.Ordinal) == -1)
			  {
				  int colonPos = line.IndexOf(':');

				  if (colonPos == -1) { buf.Append(line.Trim()); }
				  else
				    {
					    // Process field
					    string fieldName = line.Substring(0, colonPos).Trim();

              if (CultureInfo.InvariantCulture.CompareInfo.IsPrefix(fieldName, "X-", CompareOptions.Ordinal))
                { fieldName = fieldName.Substring(2); }

					    string fieldValue = line.Substring(colonPos + 1).Trim();

					    headers.Add(new PemHeader(fieldName, fieldValue));
				    }
			  }

			if (line == null) { throw new IOException(endMarker + " not found"); }

			if (buf.Length % 4 != 0) { throw new IOException("base64 data appears to be truncated"); }

			return new PemObject(type, headers, Convert.FromBase64String(buf.ToString()));
		}
    

    private static RSAParameters ReadRsaPublicKey(PemObject pemObject)
    {
      RsaPublicKeyStructure rsaPubStructure = 
        RsaPublicKeyStructure.GetInstance(
            Asn1Object.FromByteArray(pemObject.Content));

      return new RsaKeyParameters(
            false, // not private
            rsaPubStructure.Modulus, 
            rsaPubStructure.PublicExponent);
    }

    /**
     * Read a Key Pair
     */
    private object ReadPrivateKey(PemObject pemObject)
    {
      //
      // extract the key
      //
      //Debug.Assert(Platform.EndsWith(pemObject.Type, "PRIVATE KEY"));

      string type = pemObject.Type.Substring(0, pemObject.Type.Length - "PRIVATE KEY".Length).Trim();
      byte[] keyBytes = pemObject.Content;

      IDictionary fields = Platform.CreateHashtable();
      foreach (PemHeader header in pemObject.Headers)
        { fields[header.Name] = header.Value; }

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

          keyBytes = PemUtilities.Crypt(false, keyBytes, password, dekAlgName, iv);
            }

          try
            {
              AsymmetricKeyParameter pubSpec, privSpec;
              Asn1Sequence seq = Asn1Sequence.GetInstance(keyBytes);

              switch (type)
                {
                  case "RSA":
                    {
                      if (seq.Count != 9) { throw new PemException("malformed sequence in RSA private key"); }

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
                      if (seq.Count != 6) { throw new PemException("malformed sequence in DSA private key"); }

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

                    case "ENCRYPTED":
                    {
                        char[] password = pFinder.GetPassword();

                        if (password == null)
                            throw new PasswordException("Password is null, but a password is required");

                        return PrivateKeyFactory.DecryptKey(password, EncryptedPrivateKeyInfo.GetInstance(seq));
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


    private AsymmetricKeyParameter ReadPublicKey(PemObject pemObject)
    {
      return PublicKeyFactory.CreateKey(pemObject.Content);
    }
  }
}

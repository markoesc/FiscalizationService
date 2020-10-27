using MarkoEsc.FiscalizationService.CIS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MarkoEsc.FiscalizationService
{
    public static class Helper
    {
        public static void SignRequest(object request, X509Certificate2 certificate)
        {
            XmlDocument requestXml = ConvertToXml(request);
            SignedXml xml = new SignedXml(requestXml);
            
            xml.SigningKey = certificate.GetRSAPrivateKey();
            xml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            xml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

            xml.KeyInfo = new KeyInfo();
            KeyInfoX509Data keyInfoData = new KeyInfoX509Data();
            keyInfoData.AddCertificate(certificate);
            xml.KeyInfo.AddClause(keyInfoData);

            Reference reference = new Reference("");
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform(false));
            reference.AddTransform(new XmlDsigExcC14NTransform(false));
            reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
            reference.Uri = "#Request";
            xml.AddReference(reference);

            xml.ComputeSignature();

            var signature = new SignatureType
            {
                SignedInfo = new SignedInfoType
                {
                    CanonicalizationMethod = new CanonicalizationMethodType { Algorithm = SignedXml.XmlDsigExcC14NTransformUrl },
                    SignatureMethod = new SignatureMethodType { Algorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256" },
                    Reference = new ReferenceType[]
                {
                        new ReferenceType
                        {
                            URI = "#Request",
                            Transforms = new TransformType[]
                            {
                                new TransformType{ Algorithm = "http://www.w3.org/2000/09/xmldsig#enveloped-signature"},
                                new TransformType{ Algorithm = "http://www.w3.org/2001/10/xml-exc-c14n#"},
                            },
                            DigestMethod = new DigestMethodType{ Algorithm = "http://www.w3.org/2001/04/xmlenc#sha256"},
                            DigestValue = ((Reference)xml.SignedInfo.References[0]).DigestValue
                        }
                }
                },
                SignatureValue = new SignatureValueType { Value = xml.SignatureValue },
                KeyInfo = new KeyInfoType
                {
                    ItemsElementName = new ItemsChoiceType2[] { ItemsChoiceType2.X509Data },
                    Items = new object[]
                    {
                            new X509DataType {
                                ItemsElementName = new ItemsChoiceType[] { ItemsChoiceType.X509Certificate, ItemsChoiceType.X509SubjectName },
                                Items = new object[] { certificate.RawData, certificate.SubjectName.Name }
                            }
                    }
                }
            };

            var property = request.GetType().GetProperty("Signature");
            property.SetValue(request, signature, null);
        }

        public static XmlDocument ConvertToXml(object item)
        {
            XmlSerializer ser = new XmlSerializer(item.GetType(), "https://efi.tax.gov.me/fs/schema");
            XmlDocument xml = new XmlDocument();

            using (MemoryStream memStm = new MemoryStream())
            {
                ser.Serialize(memStm, item);
                memStm.Position = 0;
                using (var xtr = XmlReader.Create(memStm, new XmlReaderSettings() { IgnoreWhitespace = true }))
                {
                    xml.Load(xtr);
                }
            }

            return xml;
        }

        public static IICInfo ComputeIIC(string tin, DateTime issueDateTime, int invOrdNum, string businUnitCode, string tcrCode, string softCode, decimal totPrice, X509Certificate2 cert)
        {
            using (RSA rsa = cert.GetRSAPrivateKey())
            {
                var ikof = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}|{1:s}{1:zzz}|{2}|{3}|{4}|{5}|{6:N2}", tin, issueDateTime, invOrdNum, businUnitCode, tcrCode, softCode, totPrice);
                var signature = rsa.SignData(Encoding.UTF8.GetBytes(ikof), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var md5 = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(signature);
                return new IICInfo { IIC = ToHexString(md5), IICSignature = ToHexString(signature) };
            }
        }

        public static string ToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        public static DateTime RoundDateTime(DateTime dateTime)
        {
            return DateTime.Parse(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:s}{0:zzz}", dateTime));
        }
    }

    public class IICInfo
    {
        public string IIC { get; set; }
        public string IICSignature { get; set; }
    }
}
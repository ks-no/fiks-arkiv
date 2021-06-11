using ks.fiks.io.arkivintegrasjon.sample.messages;
using Ks.Fiks.Maskinporten.Client;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using no.ks.fiks.io.arkivmelding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using FIKS.eMeldingArkiv.eMeldingForenkletArkiv;
using Org.BouncyCastle.Asn1.X509.Qualified;

namespace ks.fiks.io.arkivsystem.sample
{
    public class XmlValidation
    {
        private List<List<string>> xmlValidationMessages = new List<List<string>>() { new List<string>(), new List<string>() };
        private const int xmlValidationErrorLimit = 25;

        public List<List<string>> ValidateXml(Stream entryStream, XmlSchemaSet xmlSchemaSet)
        {
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.ValidationType = ValidationType.Schema;
            xmlReaderSettings.Schemas.Add(xmlSchemaSet);
            xmlReaderSettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            xmlReaderSettings.ValidationEventHandler += ValidationCallBack;

            using (XmlReader validationReader = XmlReader.Create(entryStream, xmlReaderSettings))
            {
                try
                {
                    while (validationReader.Read())
                        if (xmlValidationMessages[0].Count >= xmlValidationErrorLimit)
                            break;
                }
                catch (XmlException ex)
                {
                    xmlValidationMessages[0].Add(ex.Message + " XSD validering");
                }
            }
            return xmlValidationMessages;
        }
        private void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
            {
                xmlValidationMessages[1].Add("linje " + args.Exception.LineNumber + ", posisjon " + args.Exception.LinePosition + " " + args.Message);
            }
            else if (args.Severity == XmlSeverityType.Error)
            {
                xmlValidationMessages[0].Add("linje " + args.Exception.LineNumber + ", posisjon " + args.Exception.LinePosition + " " + args.Message);
            }
        }
    }
}

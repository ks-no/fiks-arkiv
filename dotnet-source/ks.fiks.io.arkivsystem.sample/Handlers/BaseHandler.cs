using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Client.Models;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class BaseHandler
    {
        private readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        protected readonly XmlSchemaSet XmlSchemaSet;

        protected BaseHandler()
        {
            XmlSchemaSet = new XmlSchemaSet();
            var arkivModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == "KS.Fiks.Arkiv.Models.V1");
            
            using (var schemaStream =
                arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivmelding.xsd"))
            {
                using (var schemaReader = XmlReader.Create(schemaStream))
                {
                    XmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding/v2",
                        schemaReader);
                }
            }
            
            using (var schemaStream =
                arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivmeldingOppdatering.xsd"))
            {
                using (var schemaReader = XmlReader.Create(schemaStream))
                {
                    XmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/arkivmeldingoppdatering/v2",
                        schemaReader);
                }
            }
            using (var schemaStream =
                arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.dokumentfilHent.xsd"))
            {
                using (var schemaReader = XmlReader.Create(schemaStream))
                {
                    XmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/dokumentfil/hent/v2",
                        schemaReader);
                }
            }
            
            using (var schemaStream =
                arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.journalpostHent.xsd"))
            {
                using (var schemaReader = XmlReader.Create(schemaStream))
                {
                    XmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/journalpost/hent/v2",
                        schemaReader);
                }
            }
            using (var schemaStream =
                arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.mappeHent.xsd"))
            {
                using (var schemaReader = XmlReader.Create(schemaStream))
                {
                    XmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/mappe/hent/v2",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.sok.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    XmlSchemaSet.Add("http://www.ks.no/standarder/fiks/arkiv/sok/v1", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivstruktur.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    XmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/arkivstruktur",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    XmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2",
                        schemaReader);
                }
            }
        }

        protected string GetPayloadAsString(MottattMeldingArgs mottatt, XmlSchemaSet xmlSchemaSet,
            out bool xmlValidationErrorOccured, out List<List<string>> validationResult)
        {
            xmlValidationErrorOccured = false;

            IAsicReader reader = new AsiceReader();
            using (var inputStream = mottatt.Melding.DecryptedStream.Result)
            using (var asice = reader.Read(inputStream))
            {
                foreach (var asiceReadEntry in asice.Entries)
                {
                    using (var entryStream = asiceReadEntry.OpenStream())
                    {
                        if (asiceReadEntry.FileName.Contains(".xml"))
                        {
                            validationResult = new XmlValidation().ValidateXml(
                                entryStream,
                                xmlSchemaSet
                            );
                            if (validationResult[0].Count > 0)
                            {
                                xmlValidationErrorOccured = true;
                            }

                            var newEntryStream = asiceReadEntry.OpenStream();
                            var reader1 = new StreamReader(newEntryStream);
                            return reader1.ReadToEnd();
                        }
                    }

                    Log.Information("Mottatt vedlegg: {Filename}", asiceReadEntry.FileName);
                }
            }
            validationResult = null;
            return string.Empty;
        }

        protected Arkivmelding TryGetLagretArkivmelding(MottattMeldingArgs mottatt)
        {
            
            // Er det en testSession fra integrasjonstester? 
            if (mottatt.Melding.Headere.TryGetValue(ArkivSimulator.TestSessionIdHeader, out var testSessionId))
            {
                return ArkivSimulator._arkivmeldingCache.ContainsKey(testSessionId) ? ArkivSimulator._arkivmeldingCache[testSessionId] : null;
            }

            // Er det test fra protokoll-validator?
            if (mottatt.Melding.Headere.TryGetValue(ArkivSimulator.ValidatorTestNameHeader, out var testName)) 
            {
                return ArkivSimulator._arkivmeldingProtokollValidatorStorage.ContainsKey(testName) ? ArkivSimulator._arkivmeldingProtokollValidatorStorage[testName] : null;
            }

            return null;
        }
    }
}
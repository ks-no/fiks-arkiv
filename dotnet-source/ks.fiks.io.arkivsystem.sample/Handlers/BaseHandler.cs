using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Oppdatering;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.Fiks.ASiC_E;
using KS.Fiks.IO.Client.Models;
using Serilog;
using EksternNoekkel = KS.Fiks.Arkiv.Models.V1.Arkivstruktur.EksternNoekkel;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class BaseHandler
    {
        private readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        protected readonly XmlSchemaSet XmlSchemaSet;

        protected BaseHandler()
        {
            XmlSchemaSet = new XmlSchemaSet();
            var arkivModelsAssembly = Assembly.Load("KS.Fiks.Arkiv.Models.V1");
            
            using (var schemaStream =
                arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.arkivering.arkivmelding.xsd"))
            {
                using (var schemaReader = XmlReader.Create(schemaStream))
                {
                    XmlSchemaSet.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/arkivmelding/v1",
                        schemaReader);
                }
            }
            
            using (var schemaStream =
                arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.arkivering.arkivmelding.oppdater.xsd"))
            {
                using (var schemaReader = XmlReader.Create(schemaStream))
                {
                    XmlSchemaSet.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/arkivmeldingoppdatering/v1",
                        schemaReader);
                }
            }
            using (var schemaStream =
                arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.innsyn.dokumentfil.hent.xsd"))
            {
                using (var schemaReader = XmlReader.Create(schemaStream))
                {
                    XmlSchemaSet.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/dokumentfil/hent/v1",
                        schemaReader);
                }
            }
            
            using (var schemaStream =
                arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.innsyn.jounalpost.hent.xsd"))
            {
                using (var schemaReader = XmlReader.Create(schemaStream))
                {
                    XmlSchemaSet.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/journalpost/hent/v1",
                        schemaReader);
                }
            }
            using (var schemaStream =
                arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.innsyn.mappe.hent.xsd"))
            {
                using (var schemaReader = XmlReader.Create(schemaStream))
                {
                    XmlSchemaSet.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/mappe/hent/v1",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.innsyn.sok.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    XmlSchemaSet.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/sok/v1", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivstruktur.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    XmlSchemaSet.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/arkivstruktur/v1",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    XmlSchemaSet.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/metadatakatalog/v1",
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
        
        protected Arkivmelding AreEqual(MottattMeldingArgs mottatt)
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
        
        protected static bool AreEqual(Registrering lagretRegistrering, EksternNoekkel eksternNoekkel, SystemID systemId)
        {
            if (eksternNoekkel != null && lagretRegistrering.ReferanseEksternNoekkel != null)
            {
                if (lagretRegistrering.ReferanseEksternNoekkel.Fagsystem ==
                    eksternNoekkel.Fagsystem &&
                    lagretRegistrering.ReferanseEksternNoekkel.Noekkel ==
                    eksternNoekkel.Noekkel)
                {
                    return true;
                }
            }
            else if (systemId != null && lagretRegistrering.SystemID != null)
            {
                if (lagretRegistrering.SystemID.Value == systemId.Value)
                {
                    return true;
                }
            }
            return false;
        }
        
        protected static bool AreEqual(Registrering lagretRegistrering,  KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.EksternNoekkel eksternNoekkel, SystemID systemId)
        {
            if (eksternNoekkel != null && lagretRegistrering.ReferanseEksternNoekkel != null)
            {
                if (lagretRegistrering.ReferanseEksternNoekkel.Fagsystem ==
                    eksternNoekkel.Fagsystem &&
                    lagretRegistrering.ReferanseEksternNoekkel.Noekkel ==
                    eksternNoekkel.Noekkel)
                {
                    return true;
                }
            }
            else if (systemId != null && lagretRegistrering.SystemID != null)
            {
                if (lagretRegistrering.SystemID.Value == systemId.Value)
                {
                    return true;
                }
            }
            return false;
        }
        
        protected static bool AreEqual(Mappe lagretMappe, MappeOppdatering mappeOppdatering)
        {
            if (mappeOppdatering.ReferanseEksternNoekkel != null && lagretMappe.ReferanseEksternNoekkel != null)
            {
                if (lagretMappe.ReferanseEksternNoekkel.Fagsystem ==
                    mappeOppdatering.ReferanseEksternNoekkel.Fagsystem &&
                    lagretMappe.ReferanseEksternNoekkel.Noekkel ==
                    mappeOppdatering.ReferanseEksternNoekkel.Noekkel)
                {
                    return true;
                }
            }
            else if (mappeOppdatering.SystemID != null && lagretMappe.SystemID != null)
            {
                if (lagretMappe.SystemID == mappeOppdatering.SystemID)
                {
                    return true;
                }
            }
            return false;
        }
        
        protected static bool AreEqual(Mappe lagretMappe, MappeHent mappeHent)
        {
            if (mappeHent.ReferanseEksternNoekkel != null && lagretMappe.ReferanseEksternNoekkel != null)
            {
                if (lagretMappe.ReferanseEksternNoekkel.Fagsystem ==
                    mappeHent.ReferanseEksternNoekkel.Fagsystem &&
                    lagretMappe.ReferanseEksternNoekkel.Noekkel ==
                    mappeHent.ReferanseEksternNoekkel.Noekkel)
                {
                    return true;
                }
            }
            else if (mappeHent.SystemID != null && lagretMappe.SystemID != null)
            {
                if (lagretMappe.SystemID == mappeHent.SystemID)
                {
                    return true;
                }
            }
            return false;
        }
        
        protected static void SetMissingSystemID(Arkivmelding arkivmelding)
        {
            foreach (var registrering in arkivmelding.Registrering)
            {
                if (registrering.SystemID?.Value == null)
                {
                    registrering.SystemID = new SystemID() { Value = Guid.NewGuid().ToString() };
                }
            }

            foreach (var mappe in arkivmelding.Mappe)
            {
                if (mappe.SystemID?.Value == null)
                {
                    mappe.SystemID = new SystemID() { Value = Guid.NewGuid().ToString() };
                }
            }
        }

    }
}
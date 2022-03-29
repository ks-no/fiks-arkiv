using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.IO.Arkiv.Client.Models;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.IO.Arkiv.Client.Models.Innsyn.Hent.Journalpost;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class ArkivmeldingHandler : BaseHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);

        public static Arkivmelding GetPayload(MottattMeldingArgs mottatt, XmlSchemaSet xmlSchemaSet,
            out bool xmlValidationErrorOccured, out List<List<string>> validationResult)
        {
            if (mottatt.Melding.HasPayload)
            {
                var text = GetPayloadAsString(mottatt, xmlSchemaSet, out xmlValidationErrorOccured,
                    out validationResult);
                Log.Information("Parsing arkivmelding: {Xml}", text);
                if (string.IsNullOrEmpty(text))
                {
                    Log.Error("Tom arkivmelding? Xml: {Xml}", text);
                }

                using var textReader = (TextReader)new StringReader(text);
                return(Arkivmelding) new XmlSerializer(typeof(Arkivmelding)).Deserialize(textReader);
            }

            xmlValidationErrorOccured = false;
            validationResult = null;
            return null;
        }
        
        public static Melding HandleMelding(MottattMeldingArgs mottatt)
        {
            var arkivmeldingXmlSchemaSet = new XmlSchemaSet();
            arkivmeldingXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding/v2",
                Path.Combine("Schema", "arkivmelding.xsd"));
            arkivmeldingXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2",
                Path.Combine("Schema", "metadatakatalog.xsd"));

            Arkivmelding arkivmelding;
            if (mottatt.Melding.HasPayload)
            {
                arkivmelding = ArkivmeldingHandler.GetPayload(mottatt, arkivmeldingXmlSchemaSet,
                    out var xmlValidationErrorOccured, out var validationResult);

                if (xmlValidationErrorOccured) // Ugyldig forespørsel
                {
                    return new Melding
                    {
                        ResultatMelding = FeilmeldingGenerator.CreateUgyldigforespoerselMelding(validationResult),
                        FileName = "payload.json",
                        MeldingsType = FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                    };
                }
            }
            else
            {
                return new Melding
                {
                    ResultatMelding = FeilmeldingGenerator.CreateUgyldigforespoerselMelding("Arkivmelding meldingen mangler innhold"),
                    FileName = "payload.json",
                    MeldingsType = FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                };
            }

            var kvittering = new ArkivmeldingKvittering
            {
                Tidspunkt = DateTime.Now
            };
            var isMappe = arkivmelding?.Mappe?.Count > 0;

            if (isMappe)
            {
                kvittering.MappeKvittering.Add(ArkivmeldingKvitteringGenerator.CreateSaksmappeKvittering());
            }
            else
            {
                kvittering.RegistreringKvittering.Add(ArkivmeldingKvitteringGenerator.CreateJournalpostKvittering(arkivmelding));
            }

            // Lagre arkivmelding i "cache" hvis det er en testSessionId i headere
            if (mottatt.Melding.Headere.TryGetValue(ArkivSimulator.TestSessionIdHeader, out var testSessionId))
            {
                ArkivSimulator._arkivmeldingCache.Add(testSessionId, arkivmelding);
            }
            
            return new Melding
            {
                ResultatMelding = kvittering,
                FileName = "arkivmelding-kvittering.xml",
                MeldingsType = ArkivintegrasjonMeldingTypeV1.ArkivmeldingKvittering,
            };
        }
    }
}
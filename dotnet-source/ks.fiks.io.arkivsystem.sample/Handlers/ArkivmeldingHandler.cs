using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
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
        
        public static List<Melding> HandleMelding(MottattMeldingArgs mottatt)
        {
            var arkivmeldingXmlSchemaSet = new XmlSchemaSet();
            arkivmeldingXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding/v2",
                Path.Combine("Schema", "arkivmelding.xsd"));
            arkivmeldingXmlSchemaSet.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2",
                Path.Combine("Schema", "metadatakatalog.xsd"));

            List<Melding> meldinger = new List<Melding>();
            
            Arkivmelding arkivmelding;
            if (mottatt.Melding.HasPayload)
            {
                arkivmelding = GetPayload(mottatt, arkivmeldingXmlSchemaSet,
                    out var xmlValidationErrorOccured, out var validationResult);

                if (xmlValidationErrorOccured) // Ugyldig forespørsel
                {
                    meldinger.Add(new Melding
                    {
                        ResultatMelding = FeilmeldingGenerator.CreateUgyldigforespoerselMelding(validationResult),
                        FileName = "payload.json",
                        MeldingsType = FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                    });
                    return meldinger;
                }
            }
            else
            {
                meldinger.Add(new Melding
                {
                    ResultatMelding =
                        FeilmeldingGenerator.CreateUgyldigforespoerselMelding("Arkivmelding meldingen mangler innhold"),
                    FileName = "payload.json",
                    MeldingsType = FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                });
                return meldinger;
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
            
            // Det skal sendes også en mottatt melding
            meldinger.Add(new Melding
            {
                MeldingsType = FiksArkivV1Meldingtype.ArkivmeldingMottatt
            });
            
            meldinger.Add(new Melding
            {
                ResultatMelding = kvittering,
                FileName = "arkivmelding-kvittering.xml",
                MeldingsType = FiksArkivV1Meldingtype.ArkivmeldingKvittering
            });
            
            return meldinger;
        }
    }
}
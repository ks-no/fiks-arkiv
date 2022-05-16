using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.Protokoller.V1.Models.Feilmelding;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class ArkivmeldingHandler : BaseHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        
        private Arkivmelding GetPayload(MottattMeldingArgs mottatt, XmlSchemaSet xmlSchemaSet,
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
        
        public List<Melding> HandleMelding(MottattMeldingArgs mottatt)
        {
            var meldinger = new List<Melding>();
            
            Arkivmelding arkivmelding;
            if (mottatt.Melding.HasPayload)
            {
                arkivmelding = GetPayload(mottatt, XmlSchemaSet,
                    out var xmlValidationErrorOccured, out var validationResult);

                if (xmlValidationErrorOccured) // Ugyldig forespørsel
                {
                    Log.Information($"Xml validering feilet {validationResult}");
                    meldinger.Add(new Melding
                    {
                        ResultatMelding = FeilmeldingGenerator.CreateUgyldigforespoerselMelding(validationResult),
                        FileName = "payload.json",
                        MeldingsType = FeilmeldingType.Ugyldigforespørsel,
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
                    MeldingsType = FeilmeldingType.Ugyldigforespørsel,
                });
                return meldinger;
            }

            SetMissingSystemID(arkivmelding);
            var kvittering = ArkivmeldingKvitteringGenerator.CreateArkivmeldingKvittering(arkivmelding);
            
            // Lagre arkivmelding i "cache" hvis det er en testSessionId i headere
            if (mottatt.Melding.Headere.TryGetValue(ArkivSimulator.TestSessionIdHeader, out var testSessionId))
            {
                if (ArkivSimulator._arkivmeldingCache.ContainsKey(testSessionId))
                {
                    Arkivmelding lagretArkvivmelding;
                    ArkivSimulator._arkivmeldingCache.TryGetValue(testSessionId, out lagretArkvivmelding);
                    
                    if (arkivmelding.Registrering.Count >= 0)
                    {
                        foreach (var registrering in arkivmelding.Registrering)
                        {
                            if (registrering.ReferanseForelderMappe != null)
                            {
                                foreach (var lagretMappe in lagretArkvivmelding.Mappe)
                                {
                                    if (lagretMappe.SystemID.Value == registrering.ReferanseForelderMappe.Value)
                                    {
                                        lagretMappe.Registrering.Add(registrering);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    ArkivSimulator._arkivmeldingCache.Add(testSessionId, arkivmelding);
                }
            }
            
            // Det skal sendes også en mottatt melding
            meldinger.Add(new Melding
            {
                MeldingsType = FiksArkivMeldingtype.ArkivmeldingMottatt
            });
            
            meldinger.Add(new Melding
            {
                ResultatMelding = kvittering,
                FileName = "arkivmelding-kvittering.xml",
                MeldingsType = FiksArkivMeldingtype.ArkivmeldingKvittering
            });
            
            return meldinger;
        }
    }
}
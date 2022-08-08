using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Innsyn.Hent.Mappe;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Models;
using ks.fiks.io.arkivsystem.sample.Storage;
using KS.Fiks.IO.Client.Models;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class MappeHentHandler : BaseHandler, IMeldingHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);
        
        public MappeHentHandler(IArkivmeldingCache arkivmeldingCache) : base(arkivmeldingCache)
        {
        }
        
        private MappeHent GetPayload(MottattMeldingArgs mottatt, XmlSchemaSet xmlSchemaSet,
            out bool xmlValidationErrorOccured, out List<List<string>> validationResult)
        {
            if (mottatt.Melding.HasPayload)
            {
                var text = GetPayloadAsString(mottatt, xmlSchemaSet, out xmlValidationErrorOccured,
                    out validationResult);
                Log.Information("Parsing mappeHent: {Xml}", text);
                if (string.IsNullOrEmpty(text))
                {
                    Log.Error("Tom mappeHent? Xml: {Xml}", text);
                }

                using var textReader = (TextReader)new StringReader(text);
                return(MappeHent) new XmlSerializer(typeof(MappeHent)).Deserialize(textReader);
            }

            xmlValidationErrorOccured = false;
            validationResult = null;
            return null;
        }

        public List<Melding> HandleMelding(MottattMeldingArgs mottatt)
        {
            var meldinger = new List<Melding>();
            
            var hentMelding = GetPayload(mottatt, XmlSchemaSet,
                out var xmlValidationErrorOccured, out var validationResult);

            if (xmlValidationErrorOccured)
            {
                meldinger.Add(new Melding
                {
                    ResultatMelding = FeilmeldingGenerator.CreateUgyldigforespoerselMelding(validationResult),
                    FileName = "feilmelding.xml",
                    MeldingsType = FiksArkivMeldingtype.Ugyldigforespørsel,
                });
                return meldinger;
            }

            // Forsøk å hente arkivmelding fra lokal lagring
            var lagretArkivmeldinger = TryGetLagretArkivmeldinger(mottatt);
            var lagretArkivmelding = GetArkivmeldingMedMappe(lagretArkivmeldinger, hentMelding);
            
            if (lagretArkivmelding == null)
            {
                meldinger.Add(new Melding
                {
                    ResultatMelding = FeilmeldingGenerator.CreateIkkefunnetMelding("Kunne ikke finne noen mappe som tilsvarer det som er etterspurt i hentmelding"),
                    FileName = "feilmelding.xml",
                    MeldingsType = FiksArkivMeldingtype.Ikkefunnet,
                });
                return meldinger;
            }
            
            meldinger.Add(new Melding
            {
                ResultatMelding = lagretArkivmelding == null
                    ? MappeHentResultatGenerator.Create(hentMelding)
                    : MappeHentResultatGenerator.CreateFromCache(hentMelding, lagretArkivmelding),
                FileName = "resultat.xml",
                MeldingsType = FiksArkivMeldingtype.MappeHentResultat
            });
            return meldinger;
        }
        
        private Arkivmelding GetArkivmeldingMedMappe(List<Arkivmelding> lagretArkivmeldinger, MappeHent mappeHent)
        {
            if (lagretArkivmeldinger == null)
            {
                return null;
            }

            foreach (var lagretArkivmelding in lagretArkivmeldinger)
            {
                if (lagretArkivmelding.Mappe.Count >= 0)
                {
                    foreach (var mappe in lagretArkivmelding.Mappe)
                    {
                        if (AreEqual(mappe, mappeHent))
                        {
                            return lagretArkivmelding;
                        }

                    }
                }
            }

            return null;
        }
    }
}
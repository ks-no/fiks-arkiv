using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding.Oppdatering;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using ks.fiks.io.arkivsystem.sample.Generators;
using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;
using Serilog;

namespace ks.fiks.io.arkivsystem.sample.Handlers
{
    public class ArkivmeldingOppdaterHandler : BaseHandler
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(MethodBase.GetCurrentMethod()?.DeclaringType);

        public List<Melding> HandleMelding(MottattMeldingArgs mottatt)
        {
            var meldinger = new List<Melding>();
            var arkivmeldingOppdatering = new ArkivmeldingOppdatering();

            if (!mottatt.Melding.HasPayload)
            {
                meldinger.Add(new Melding
                {
                    ResultatMelding =
                        FeilmeldingGenerator.CreateUgyldigforespoerselMelding(
                            "ArkivmeldingOppdatering meldingen mangler innhold"),
                    FileName = "payload.json",
                    MeldingsType = FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                });
                return meldinger;
            }

            arkivmeldingOppdatering = GetPayload(mottatt, XmlSchemaSet,
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

            // Melding er validert i henhold til xsd, vi sender tilbake mottatt melding
            meldinger.Add(new Melding
            {
                MeldingsType = FiksArkivV1Meldingtype.ArkivmeldingMottatt,
            });

            Arkivmelding lagretArkivmelding = null;
            if (mottatt.Melding.Headere.TryGetValue(ArkivSimulator.TestSessionIdHeader, out var testSessionId))
            {
                ArkivSimulator._arkivmeldingCache.TryGetValue(testSessionId, out lagretArkivmelding);
            }
            else if (ArkivSimulator._arkivmeldingProtokollValidatorStorage.ContainsKey(arkivmeldingOppdatering.MeldingId)) {
                lagretArkivmelding =
                    ArkivSimulator._arkivmeldingProtokollValidatorStorage[arkivmeldingOppdatering.MeldingId];
            } 
        
            if(lagretArkivmelding != null) {
                try
                {
                    if (arkivmeldingOppdatering.RegistreringOppdateringer.Count > 0)
                    {
                        meldinger.AddRange(OppdaterRegistreringer(arkivmeldingOppdatering, lagretArkivmelding));
                    }
                    else if (arkivmeldingOppdatering.MappeOppdateringer.Count > 0) // Mappe oppdatering
                    {
                        meldinger.AddRange(OppdaterMapper(arkivmeldingOppdatering, lagretArkivmelding));
                    }
                }
                catch (Exception e)
                {
                    meldinger.Add(new Melding
                    {
                        ResultatMelding =
                            FeilmeldingGenerator.CreateServerFeilMelding(
                                $"Noe gikk galt: {e.Message}"),
                        FileName = "payload.json",
                        MeldingsType = FeilmeldingMeldingTypeV1.Serverfeil,
                    });
                    return meldinger;
                }
            }
            else
            {
                // Fant ikke noen melding å oppdatere
                meldinger.Add(new Melding
                {
                    ResultatMelding = FeilmeldingGenerator.CreateUgyldigforespoerselMelding(
                        "ArkivmeldingOppdatering ikke gyldig. Kunne ikke finne noe registrert i arkivet med gitt id"),
                    FileName = "payload.json",
                    MeldingsType = FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                });
                return meldinger;
            }
            
            return meldinger;
        }

        private static List<Melding> OppdaterMapper(ArkivmeldingOppdatering arkivmeldingOppdatering,
            Arkivmelding lagretArkivmelding)
        {
            var meldinger = new List<Melding>();
            var found = false;
            var success = false;
            foreach (var mappeOppdatering in arkivmeldingOppdatering.MappeOppdateringer)
            {
                foreach (var lagretMappe in lagretArkivmelding.Mappe)
                {
                    if(AreEqual(lagretMappe, mappeOppdatering))
                    {
                        if(mappeOppdatering.Tittel != null) { lagretMappe.Tittel = mappeOppdatering.Tittel; }
                        if(mappeOppdatering.OffentligTittel != null) { lagretMappe.OffentligTittel = mappeOppdatering.OffentligTittel; }
                        if(mappeOppdatering.Beskrivelse != null) { lagretMappe.Beskrivelse = mappeOppdatering.Beskrivelse; }
                        
                        //TODO Etter hvert: legge til mulighet for å kunne oppdatere virksomhetsspesifikkeMetadata og partOppdatering + evt andre som mangler her
                        
                        found = true;
                        
                        if (mappeOppdatering is SaksmappeOppdatering oppdatering)
                        {
                            if (oppdatering.Saksansvarlig != null)
                            {
                                ((Saksmappe)lagretMappe).Saksansvarlig = oppdatering.Saksansvarlig;
                            }
                            //TODO Etter hvert: legge til resten av oppdateringsmuligheter for Saksmappe
                        }

                        success = true;
                    }
                }

                if (!found)
                {
                    Log.Warning("Fant ikke noen mappe å oppdatere basert på enten SystemID eller ReferanseEksternNoekkel");
                    meldinger.Add(new Melding
                    {
                        ResultatMelding =
                            FeilmeldingGenerator.CreateUgyldigforespoerselMelding(
                                "Mangler enten ReferanseEksternNoekkel eller SystemID for enten lagret mappe i 'arkivet' eller innkommende oppdatering. Kunne ikke matche forespørsel."),
                        FileName = "payload.json",
                        MeldingsType = FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                    });
                }
            }

            if (success)
            {
                // Kvittering melding
                meldinger.Add(new Melding
                {
                    MeldingsType = FiksArkivV1Meldingtype.ArkivmeldingOppdaterKvittering,
                });
            }

            return meldinger;
        }

        private static List<Melding> OppdaterRegistreringer(ArkivmeldingOppdatering arkivmeldingOppdatering,
            Arkivmelding lagretArkivmelding)
        {
            var success = false;
            var meldinger = new List<Melding>();
            foreach (var registreringOppdatering in arkivmeldingOppdatering.RegistreringOppdateringer)
            {
                var found = false;
                foreach (var registrering in lagretArkivmelding.Registrering)
                {
                    if (AreEqual(registrering, registreringOppdatering))
                    {
                        if(registreringOppdatering.Tittel != null) { registrering.Tittel = registreringOppdatering.Tittel; }
                        if(registreringOppdatering.OffentligTittel != null) { registrering.OffentligTittel = registreringOppdatering.OffentligTittel; }
                        if(registreringOppdatering.Skjerming != null) { registrering.Skjerming = registreringOppdatering.Skjerming; }
                        if(registreringOppdatering.Gradering != null) { registrering.Gradering = registreringOppdatering.Gradering; }
                        
                        //TODO legge til de som mangler
                        
                        found = true;
                        success = true;
                    }   
                }

                if (!found)
                {
                    meldinger.Add(new Melding
                    {
                        ResultatMelding =
                            FeilmeldingGenerator.CreateUgyldigforespoerselMelding(
                                "Mangler enten ReferanseEksternNoekkel eller SystemID for enten lagret registrering i 'arkivet' eller innkommende oppdatering. Kunne ikke matche forespørsel."),
                        FileName = "payload.json",
                        MeldingsType = FeilmeldingMeldingTypeV1.Ugyldigforespørsel,
                    });
                }
            }
            
            if (success)
            {
                // Kvittering melding
                meldinger.Add(new Melding
                {
                    MeldingsType = FiksArkivV1Meldingtype.ArkivmeldingOppdaterKvittering,
                });
            }
            return meldinger;
        }

        private static bool AreEqual(Registrering lagretRegistrering, RegistreringOppdatering registreringOppdatering)
        {
            if (registreringOppdatering.ReferanseEksternNoekkel != null && lagretRegistrering.ReferanseEksternNoekkel != null)
            {
                if (lagretRegistrering.ReferanseEksternNoekkel.Fagsystem ==
                    registreringOppdatering.ReferanseEksternNoekkel.Fagsystem &&
                    lagretRegistrering.ReferanseEksternNoekkel.Noekkel ==
                    registreringOppdatering.ReferanseEksternNoekkel.Noekkel)
                {
                    return true;
                }
            }
            else if (registreringOppdatering.SystemID != null && lagretRegistrering.SystemID != null)
            {
                if (lagretRegistrering.SystemID == registreringOppdatering.SystemID)
                {
                    return true;
                }
            }
            return false;
        }
        
        private static bool AreEqual(Mappe lagretMappe, MappeOppdatering mappeOppdatering)
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

        private ArkivmeldingOppdatering GetPayload(MottattMeldingArgs mottatt, XmlSchemaSet xmlSchemaSet,
            out bool xmlValidationErrorOccured, out List<List<string>> validationResult)
        {
            if (mottatt.Melding.HasPayload)
            {
                var text = GetPayloadAsString(mottatt, xmlSchemaSet, out xmlValidationErrorOccured,
                    out validationResult);
                Log.Information("Parsing arkivmeldingOppdatering: {Xml}", text);
                if (string.IsNullOrEmpty(text))
                {
                    Log.Error("Tom arkivmeldingOppdatering? Xml: {Xml}", text);
                }

                using var textReader = (TextReader)new StringReader(text);
                return (ArkivmeldingOppdatering)new XmlSerializer(typeof(ArkivmeldingOppdatering)).Deserialize(
                    textReader);
            }

            xmlValidationErrorOccured = false;
            validationResult = null;
            return null;
        }
    }
}
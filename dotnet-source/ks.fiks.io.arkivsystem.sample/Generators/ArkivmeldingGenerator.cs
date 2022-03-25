using System;
using System.IO;
using System.Xml.Schema;
using KS.Fiks.IO.Arkiv.Client.Models;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding;
using KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.IO.Arkiv.Client.Models.Metadatakatalog;
using ks.fiks.io.arkivsystem.sample.Handlers;
using ks.fiks.io.arkivsystem.sample.Models;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Models.Feilmelding;
using Dokumentobjekt = KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding.Dokumentobjekt;
using EksternNoekkel = KS.Fiks.IO.Arkiv.Client.Models.Arkivering.Arkivmelding.EksternNoekkel;

namespace ks.fiks.io.arkivsystem.sample.Generators
{
    public static class ArkivmeldingGenerator
    {
        public static Melding CreateArkivmelding(MottattMeldingArgs mottatt)
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

        public static Journalpost CreateJournalpost()
        {
            return new Journalpost()
            {
                OpprettetAv = "En brukerid",
                ArkivertAv = "En brukerid",
                ReferanseForelderMappe = new SystemID() { Label = "", Value = Guid.NewGuid().ToString() },
                ReferanseEksternNoekkel = new EksternNoekkel()
                {
                    Fagsystem = "Fagsystem X",
                    Noekkel = Guid.NewGuid().ToString()
                },
                Dokumentbeskrivelse =
                {
                    new Dokumentbeskrivelse()
                    {
                        Dokumenttype = "SØKNAD",
                        Dokumentstatus = "F",
                        Tittel = "Rekvisisjon av oppmålingsforretning",
                        TilknyttetRegistreringSom = "H",
                        Dokumentobjekt =
                        {
                            new Dokumentobjekt()
                            {
                                Versjonsnummer = "1",
                                Variantformat = "P",
                                Format = "PDF",
                                Filnavn = "rekvisjon.pdf",
                                ReferanseDokumentfil = "rekvisisjon.pdf"
                            }
                        }
                    }
                },
                Tittel = "Internt notat",
                Korrespondansepart =
                {
                    new Korrespondansepart()
                    {
                        Korrespondanseparttype = "IM",
                        KorrespondansepartNavn = "Oppmålingsetaten",
                        AdministrativEnhet = "Oppmålingsetaten",
                        Saksbehandler = "Ingrid Mottaker"
                    }
                },
                Journalposttype = "X",
                Journalstatus = "F",
                DokumentetsDato = DateTime.Now.Date,
                MottattDato = DateTime.Now,
            };
        }
    }
}
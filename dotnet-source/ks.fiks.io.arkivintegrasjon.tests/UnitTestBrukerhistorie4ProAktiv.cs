using System;
using System.Collections.Generic;
using System.Globalization;
using ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering;
using no.ks.fiks.io.arkivmelding.sok;
using NUnit.Framework;
using FIKS.eMeldingArkiv.eMeldingForenkletArkiv;

namespace ks.fiks.io.arkivintegrasjon.tests
{
    class UnitTestBrukerhistorie4ProAktiv
    {
        [SetUp]
        public void Setup()
        {
        }
        // Bruker en ekisterende sak dersom det finnes en sak av rett type hvis ikke opprettes ny sak
        [Test]
        public void TestNyttDokumentBrukEksisterendeSak()
        {

            Saksmappe[] saker = FinnSakerMedMatrikkelnummer("21/400");
            Saksmappe sak = null;
            foreach (Saksmappe testSak in saker)
            {
                // sjekk om testSak er av rett type i tilfelle sett sak til testSak
            }


            if (sak == null)
            {
                sak = OpprettNySak();
            }

            object jp = OpprettJournalpostMedDokument(sak);

            

            Assert.Pass();
        }

       
        private object OpprettJournalpostMedDokument(Saksmappe saksmappe)
        {
            //var messageRequest = new MeldingRequest(
            //                         mottakerKontoId: receiverId,
            //                         avsenderKontoId: senderId,
            //                         meldingType: "no.geointegrasjon.arkiv.oppdatering.arkivmeldingforenkletUtgaaende.v1"); // Message type as string
            //                                                                                                                //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema


            //Fagsystem definerer ønsket struktur
            ArkivmeldingForenkletUtgaaende utg = new ArkivmeldingForenkletUtgaaende
            {
                sluttbrukerIdentifikator = "ABC", 
                nyUtgaaendeJournalpost = new UtgaaendeJournalpost()
            };

            utg.referanseSaksmappe = saksmappe;
            

            utg.nyUtgaaendeJournalpost.tittel = "Vedtak etter tilsyn";
            utg.nyUtgaaendeJournalpost.referanseEksternNoekkel = new EksternNoekkel
            {
                fagsystem = "Fagsystem X",
                noekkel = new Guid().ToString()
            };

            utg.nyUtgaaendeJournalpost.internAvsender = new List<KorrespondansepartIntern>
            {
                new KorrespondansepartIntern() {
                    saksbehandler = "Birger Brannmann",
                    referanseSaksbehandler = "60577438-1f97-4c5f-b254-aa758c8786c4"
                }
            };

            utg.nyUtgaaendeJournalpost.mottaker = new List<Korrespondansepart>
            {
                new Korrespondansepart() {
                    navn = "Mons Mottaker",
                    personid = new Personidentifikator() { personidentifikatorType = "F",  personidentifikatorNr = "12345678901"},
                    postadresse = new EnkelAdresse() {
                        adresselinje1 = "Gate 1",
                        adresselinje2 = "Andre adresselinje",
                        adresselinje3 = "Tredje adresselinje",
                        postnr = "3801",
                        poststed = "Bø" },
                    forsendelsemåte = "SvarUt"
                }
            };

            utg.nyUtgaaendeJournalpost.hoveddokument = new ForenkletDokument
            {
                tittel = "Vedtak",
                filnavn = "vedtak.pdf"
            };

           

            //Konverterer til arkivmelding xml
            var arkivmelding = Arkivintegrasjon.ConvertForenkletUtgaaendeToArkivmelding(utg);
            string payload = Arkivintegrasjon.Serialize(arkivmelding);

            ////Lager FIKS IO melding
            //List<IPayload> payloads = new List<IPayload>();
            //payloads.Add(new StringPayload(payload, "utgaaendejournalpost.xml"));
            //payloads.Add(new FilePayload(@"samples\vedtak.pdf"));
            //payloads.Add(new FilePayload(@"samples\vedlegg.pdf"));

            ////Sender til FIKS IO (arkiv løsning)
            //var msg = client.Send(messageRequest, payloads).Result;

            return null;
        }

        private Saksmappe OpprettNySak()
        {
            Saksmappe utg = new Saksmappe
            {
            tittel ="Tilsyn eiendom 21/400"
            };

            //Konverterer til arkivmelding xml
            //var arkivmelding = Arkivintegrasjon.ConvertSaksmappe(utg);
            //string payload = Arkivintegrasjon.Serialize(arkivmelding);

            //TODO returner saken som ble opprettet
            return null;
        }

        private Saksmappe[] FinnSakerMedMatrikkelnummer(string matrikkelnummer)
        {
            var arkivmeldingsok = new sok
            {
                respons = respons_type.mappe,
                meldingId = Guid.NewGuid().ToString(),
                system = "Fagsystem X",
                tidspunkt = DateTime.Now,
                skip = 0,
                take = 100
            };

            List<parameter> paramlist = new List<parameter>
            {
                new parameter
                {
                    felt = field_type.mappetittel,
                    @operator = operator_type.equal,
                    parameterverdier = new parameterverdier
                    {
                        Item = new klassifikasjonvalues
                        {
                            klassifikasjonssystem = new[] {"GBNR"},
                            klasse = new[] {matrikkelnummer}
                        }
                    }
                }
            };

            arkivmeldingsok.parameter = paramlist.ToArray();

            var payload = Arkivintegrasjon.Serialize(arkivmeldingsok);
            //ToDo returner saker
            return null;
        }
    }
}

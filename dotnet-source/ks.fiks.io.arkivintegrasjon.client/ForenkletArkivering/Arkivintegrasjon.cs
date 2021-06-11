﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using FIKS.eMeldingArkiv.eMeldingForenkletArkiv;
using no.ks.fiks.io.arkivmelding;

namespace ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering
{
    public class Arkivintegrasjon
    {
        private static readonly string Skjermingshjemmel = "Offl. § 26.1";
        const string MottakerKode = "EM";
        const string AvsenderKode = "EA";
        const string InternavsenderKode = "IA";
        const string InternmottakerKode = "IM";

        public static arkivmelding ConvertOppdaterSaksmappeToArkivmelding(OppdaterSaksmappe input)
        {
            if (input.oppdaterSaksmappe == null)
            {
                throw new Exception("Badrequest - saksmappe må være angitt");    
            }
            
            var arkivmld = new arkivmelding();
            var antFiler = 0;
            var mappeliste = new List<saksmappe> { ConvertSaksmappe(input.oppdaterSaksmappe) };
            
            arkivmld.Items = mappeliste.ToArray();

            arkivmld.antallFiler = antFiler;
            arkivmld.system = input.oppdaterSaksmappe.referanseEksternNoekkel?.fagsystem;
            arkivmld.meldingId = input.oppdaterSaksmappe.referanseEksternNoekkel?.noekkel;
            arkivmld.tidspunkt = DateTime.Now;

            return arkivmld;
        }
        public static arkivmelding ConvertForenkletUtgaaendeToArkivmelding(ArkivmeldingForenkletUtgaaende input) {

            if (input.nyUtgaaendeJournalpost == null)
            {
                throw new Exception("Badrequest - journalpost må være angitt");
            }

            var arkivmld = new arkivmelding();
            var antFiler = 0;
            saksmappe mappe = null;
            if (input.referanseSaksmappe != null)
            {
                mappe = ConvertSaksmappe(input.referanseSaksmappe);

            }
           
            if (input.nyUtgaaendeJournalpost != null) {
                var journalpst = new journalpost
                {
                    tittel = input.nyUtgaaendeJournalpost.tittel,
                    journalposttype = "U"
                };

                if (input.nyUtgaaendeJournalpost.journalaar > 0)
                    journalpst.journalaar = input.nyUtgaaendeJournalpost.journalaar.ToString();
                if (input.nyUtgaaendeJournalpost.journalsekvensnummer > 0)
                    journalpst.journalsekvensnummer = input.nyUtgaaendeJournalpost.journalsekvensnummer.ToString();
                if (input.nyUtgaaendeJournalpost.journalpostnummer > 0) 
                    journalpst.journalpostnummer = input.nyUtgaaendeJournalpost.journalpostnummer.ToString();

                if (input.nyUtgaaendeJournalpost.sendtDato.HasValue) {
                    journalpst.sendtDato = input.nyUtgaaendeJournalpost.sendtDato.Value;
                    journalpst.sendtDatoSpecified = true;
                }
                if (input.nyUtgaaendeJournalpost.dokumentetsDato != null)
                {
                    journalpst.dokumentetsDato = input.nyUtgaaendeJournalpost.dokumentetsDato.Value;
                    journalpst.dokumentetsDatoSpecified = true;
                }
                if (input.nyUtgaaendeJournalpost.offentlighetsvurdertDato != null)
                {
                    journalpst.offentlighetsvurdertDato = input.nyUtgaaendeJournalpost.offentlighetsvurdertDato.Value;
                    journalpst.offentlighetsvurdertDatoSpecified = true;
                }
                
                journalpst.opprettetAv = input.sluttbrukerIdentifikator;
                journalpst.arkivertAv = input.sluttbrukerIdentifikator; //TODO ?????
                
                // Skjerming
                if (input.nyUtgaaendeJournalpost.skjermetTittel)
                {
                    journalpst.skjerming = new skjerming()
                    {
                        skjermingshjemmel = Skjermingshjemmel,
                        skjermingMetadata = new List<string> { "tittel", "korrespondansepart" }.ToArray()
                    };
                }

                // Håndtere alle filer
                List<dokumentbeskrivelse> dokbliste = new List<dokumentbeskrivelse>();

                if (input.nyUtgaaendeJournalpost.hoveddokument != null)
                {
                    var dokbesk = new dokumentbeskrivelse
                    {
                        dokumentstatus = "F",
                        tilknyttetRegistreringSom = "H",
                        tittel = input.nyUtgaaendeJournalpost.hoveddokument.tittel
                    };

                    if (input.nyUtgaaendeJournalpost.hoveddokument.skjermetDokument) {
                        dokbesk.skjerming = new skjerming()
                        {
                            skjermingshjemmel = Skjermingshjemmel,
                            skjermingDokument = "Hele"
                        };
                    }
                    
                    var dok = new dokumentobjekt
                    {
                        referanseDokumentfil = input.nyUtgaaendeJournalpost.hoveddokument.filnavn
                    };
                    List<dokumentobjekt> dokliste = new List<dokumentobjekt>
                    {
                        dok
                    };

                    dokbesk.dokumentobjekt = dokliste.ToArray();

                    dokbliste.Add(dokbesk);
                    antFiler++;
                }
                foreach (var item in input.nyUtgaaendeJournalpost.vedlegg)
                {
                    var dokbesk = new dokumentbeskrivelse
                    {
                        dokumentstatus = "F",
                        tilknyttetRegistreringSom = "V",
                        tittel = item.tittel
                        
                    };

                    var dok = new dokumentobjekt
                    {
                        referanseDokumentfil = item.filnavn
                    };
                    List<dokumentobjekt> dokliste = new List<dokumentobjekt>
                    {
                        dok
                    };

                    dokbesk.dokumentobjekt = dokliste.ToArray();

                    dokbliste.Add(dokbesk);
                    antFiler++;

                }
                journalpst.dokumentbeskrivelse = dokbliste.ToArray();

                // Korrespondanseparter
                List<korrespondansepart> partsListe = new List<korrespondansepart>();

                foreach (var mottaker in input.nyUtgaaendeJournalpost.mottaker)
                {
                    korrespondansepart korrpart = KorrespondansepartToArkivPart(MottakerKode, mottaker);
                    partsListe.Add(korrpart);
                }

                foreach (var avsender in input.nyUtgaaendeJournalpost.avsender)
                {
                    korrespondansepart korrpart = KorrespondansepartToArkivPart(AvsenderKode, avsender);
                    partsListe.Add(korrpart);
                }
                
                foreach (var internAvsender in input.nyUtgaaendeJournalpost.internAvsender)
                {
                    korrespondansepart korrpart = InternKorrespondansepartToArkivPart(InternavsenderKode, internAvsender);
                    partsListe.Add(korrpart);
                }

                journalpst.korrespondansepart = partsListe.ToArray();

                if (input.nyUtgaaendeJournalpost.referanseEksternNoekkel != null)
                {
                    journalpst.referanseEksternNoekkel = new eksternNoekkel();
                    journalpst.referanseEksternNoekkel.fagsystem = input.nyUtgaaendeJournalpost.referanseEksternNoekkel.fagsystem;
                    journalpst.referanseEksternNoekkel.noekkel = input.nyUtgaaendeJournalpost.referanseEksternNoekkel.noekkel;
                }

                List<journalpost> jliste = new List<journalpost>
                {
                    journalpst
                };

                if (mappe != null)
                {
                    var mappeliste = new List<saksmappe>();
                    mappe.Items = jliste.ToArray();
                    mappeliste.Add(mappe);
                    arkivmld.Items = mappeliste.ToArray();
                }
                else {
                    arkivmld.Items = jliste.ToArray();
                }
            }
            
            arkivmld.antallFiler = antFiler;
            arkivmld.system = input.nyUtgaaendeJournalpost.referanseEksternNoekkel?.fagsystem;
            arkivmld.meldingId = input.nyUtgaaendeJournalpost.referanseEksternNoekkel?.noekkel;
            arkivmld.tidspunkt = DateTime.Now;

            return arkivmld;
        }

        private static korrespondansepart KorrespondansepartToArkivPart(string partRolle, Korrespondansepart mottaker)
        {
            var part= new korrespondansepart
            {
                korrespondansepartNavn = mottaker.navn,
                korrespondanseparttype = partRolle,
                postadresse = (new List<string>() {
                    mottaker.postadresse?.adresselinje1,
                    mottaker.postadresse?.adresselinje2,
                    mottaker.postadresse?.adresselinje3
                }).ToArray(),
                land = mottaker.postadresse?.landkode,
                postnummer = mottaker.postadresse?.postnr,
                poststed = mottaker.postadresse?.poststed,
                kontaktperson = mottaker.kontaktperson,
                epostadresse = mottaker.kontaktinformasjon?.epostadresse,
                telefonnummer = (new List<string>() {
                    mottaker.kontaktinformasjon?.mobiltelefon,
                    mottaker.kontaktinformasjon?.telefon
                }).ToArray(),
                deresReferanse = mottaker.deresReferanse,
                forsendelsesmaate = mottaker.forsendelsemåte
            };

            if (mottaker.enhetsidentifikator?.organisasjonsnummer != null) {
                part.Item = new EnhetsidentifikatorType()
                    {
                        organisasjonsnummer = mottaker.enhetsidentifikator.organisasjonsnummer
                    };
            }
            
            if (mottaker.personid?.personidentifikatorNr != null) {
                if (mottaker.personid?.personidentifikatorType == "F")
                {
                    part.Item = new FoedselsnummerType()
                    {
                        foedselsnummer = mottaker.personid?.personidentifikatorNr
                    };
                }
                else {
                    part.Item = new DNummerType()
                    {
                        DNummer = mottaker.personid?.personidentifikatorNr
                    };
                }
            }

            return part;
        }
        
        private static korrespondansepart InternKorrespondansepartToArkivPart(string internKode, KorrespondansepartIntern intern)
        {
            return  new korrespondansepart
            {
                korrespondansepartNavn = intern.saksbehandler ?? intern.administrativEnhet,
                korrespondanseparttype = internKode,
                administrativEnhet = intern.administrativEnhet,
                saksbehandler = intern.saksbehandler
            };
        }

        public static string Serialize(object arkivmelding)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(arkivmelding.GetType());
            var stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, arkivmelding);
            return stringWriter.ToString();
        }

        public static arkivmelding DeSerialize(string arkivmelding)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(arkivmelding));
            arkivmelding arkivmeldingDeserialized;
            using (TextReader reader = new StringReader(arkivmelding))
            {
                arkivmeldingDeserialized = (arkivmelding) serializer.Deserialize(reader);
            }

            return arkivmeldingDeserialized;
        }

        public static arkivmelding ConvertForenkletInnkommendeToArkivmelding(ArkivmeldingForenkletInnkommende input)
        {
            if (input.nyInnkommendeJournalpost == null) throw new Exception("Badrequest - journalpost må være angitt");

            var arkivmld = new arkivmelding();
            int antFiler = 0;
            saksmappe mappe = null;

            if (input.referanseSaksmappe != null)
            {
                mappe = ConvertSaksmappe(input.referanseSaksmappe);
            }

            if (input.nyInnkommendeJournalpost != null)
            {
                var journalpst = new journalpost();
                journalpst.tittel = input.nyInnkommendeJournalpost.tittel;

                journalpst.journalposttype = "I";
                if (input.nyInnkommendeJournalpost.mottattDato != null)
                {
                    journalpst.mottattDato = input.nyInnkommendeJournalpost.mottattDato.Value;
                    journalpst.mottattDatoSpecified = true;
                }
                if (input.nyInnkommendeJournalpost.dokumentetsDato != null)
                {
                    journalpst.dokumentetsDato = input.nyInnkommendeJournalpost.dokumentetsDato.Value;
                    journalpst.dokumentetsDatoSpecified = true;
                }
                if (input.nyInnkommendeJournalpost.offentlighetsvurdertDato != null)
                {
                    journalpst.offentlighetsvurdertDato = input.nyInnkommendeJournalpost.offentlighetsvurdertDato.Value;
                    journalpst.offentlighetsvurdertDatoSpecified = true;
                }

                journalpst.offentligTittel = input.nyInnkommendeJournalpost.offentligTittel;
                journalpst.opprettetAv = input.sluttbrukerIdentifikator;
                journalpst.arkivertAv = input.sluttbrukerIdentifikator; //TODO ?????
                
                // Skjerming
                if (input.nyInnkommendeJournalpost.skjermetTittel)
                {
                    journalpst.skjerming = new skjerming()
                    {
                        skjermingshjemmel = input.nyInnkommendeJournalpost.skjerming?.skjermingshjemmel,
                        skjermingMetadata = new List<string> { "tittel", "korrespondansepart" }.ToArray()
                    };
                }
                
                // Håndtere alle filer
                List<dokumentbeskrivelse> dokbliste = new List<dokumentbeskrivelse>();
                
                if (input.nyInnkommendeJournalpost.hoveddokument != null)
                {
                    var dokbesk = new dokumentbeskrivelse
                    {
                        dokumentstatus = "F",
                        tilknyttetRegistreringSom = "H",
                        tittel = input.nyInnkommendeJournalpost.hoveddokument.tittel
                    };
                    
                    if (input.nyInnkommendeJournalpost.hoveddokument.skjermetDokument)
                    {
                        dokbesk.skjerming = new skjerming()
                        {
                            skjermingshjemmel = input.nyInnkommendeJournalpost.skjerming?.skjermingshjemmel,
                            skjermingDokument = "Hele"
                        };
                    }
                    var dok = new dokumentobjekt
                    {
                        referanseDokumentfil = input.nyInnkommendeJournalpost.hoveddokument.filnavn
                    };
                    List<dokumentobjekt> dokliste = new List<dokumentobjekt>();
                    dokliste.Add(dok);

                    dokbesk.dokumentobjekt = dokliste.ToArray();
                    
                    dokbliste.Add(dokbesk);
                    antFiler++;
                }
                foreach (var item in input.nyInnkommendeJournalpost.vedlegg)
                {
                    var dokbesk = new dokumentbeskrivelse();
                    dokbesk.dokumentstatus = "F";
                    dokbesk.tilknyttetRegistreringSom = "V";
                    dokbesk.tittel = item.tittel;

                    var dok = new dokumentobjekt();
                    dok.referanseDokumentfil = item.filnavn;
                    List<dokumentobjekt> dokliste = new List<dokumentobjekt>();
                    dokliste.Add(dok);

                    dokbesk.dokumentobjekt = dokliste.ToArray();
                    
                    dokbliste.Add(dokbesk);
                    antFiler++;

                }
                journalpst.dokumentbeskrivelse = dokbliste.ToArray();

                //Korrespondanseparter
                List<korrespondansepart> partsListe = new List<korrespondansepart>();

                foreach (var mottaker in input.nyInnkommendeJournalpost.mottaker)
                {
                    korrespondansepart korrpart = KorrespondansepartToArkivPart(MottakerKode, mottaker);
                    partsListe.Add(korrpart);
                }

                foreach (var avsender in input.nyInnkommendeJournalpost.avsender)
                {
                    korrespondansepart korrpart = KorrespondansepartToArkivPart(AvsenderKode, avsender);
                    partsListe.Add(korrpart);
                }

                foreach (var internMottaker in input.nyInnkommendeJournalpost.internMottaker)
                {
                    korrespondansepart korrpart = InternKorrespondansepartToArkivPart(InternmottakerKode, internMottaker);
                    partsListe.Add(korrpart);
                }

                journalpst.korrespondansepart = partsListe.ToArray();

                if (input.nyInnkommendeJournalpost.referanseEksternNoekkel != null)
                {
                    journalpst.referanseEksternNoekkel = new eksternNoekkel();
                    journalpst.referanseEksternNoekkel.fagsystem = input.nyInnkommendeJournalpost.referanseEksternNoekkel.fagsystem;
                    journalpst.referanseEksternNoekkel.noekkel = input.nyInnkommendeJournalpost.referanseEksternNoekkel.noekkel;
                }

                List<journalpost> jliste = new List<journalpost>
                {
                    journalpst
                };

                if (mappe != null)
                {
                    var mappeliste = new List<saksmappe>();
                    mappe.Items = jliste.ToArray();
                    mappeliste.Add(mappe);
                    arkivmld.Items = mappeliste.ToArray();
                }
                else
                {
                    arkivmld.Items = jliste.ToArray();
                }

            }
            arkivmld.antallFiler = antFiler;
            arkivmld.system = input.nyInnkommendeJournalpost.referanseEksternNoekkel.fagsystem;
            arkivmld.meldingId = input.nyInnkommendeJournalpost.referanseEksternNoekkel.noekkel;
            arkivmld.tidspunkt = DateTime.Now;

            return arkivmld;
        }

        public static arkivmelding ConvertForenkletNotatToArkivmelding(ArkivmeldingForenkletNotat input)
        {
            if (input.nyttNotat == null) throw new Exception("Badrequest -notat må være angitt");

            var arkivmld = new arkivmelding();
            int antFiler = 0;
            saksmappe mappe = null;

            if (input.referanseSaksmappe != null)
            {
                mappe = ConvertSaksmappe(input.referanseSaksmappe);

            }

            if (input.nyttNotat != null)
            {
                var journalpst = new journalpost();
                journalpst.tittel = input.nyttNotat.tittel;

                journalpst.opprettetAv = input.sluttbrukerIdentifikator;
                journalpst.arkivertAv = input.sluttbrukerIdentifikator; //TODO ?????

                journalpst.journalposttype = "N";
                //if (input.nyttNotat.mottattDato != null)
                //{
                //    journalpst.mottattDato = input.nyttNotat.mottattDato.Value;
                //    journalpst.mottattDatoSpecified = true;
                //}
                if (input.nyttNotat.dokumentetsDato != null)
                {
                    journalpst.dokumentetsDato = input.nyttNotat.dokumentetsDato.Value;
                    journalpst.dokumentetsDatoSpecified = true;
                }
                //if (input.nyttNotat.offentlighetsvurdertDato != null)
                //{
                //    journalpst.offentlighetsvurdertDato = input.nyttNotat.offentlighetsvurdertDato.Value;
                //    journalpst.offentlighetsvurdertDatoSpecified = true;
                //}

                //journalpst.offentligTittel = input.nyttNotat.offentligTittel;

                ////skjerming
                //if (input.nyttNotat.skjermetTittel)
                //{
                //    journalpst.skjerming = new skjerming()
                //    {
                //        skjermingshjemmel = input.nyttNotat.skjerming?.skjermingshjemmel,
                //        skjermingMetadata = new List<string> { "tittel", "korrespondansepart" }.ToArray()
                //    };
                //}
                //Håndtere alle filer
                List<dokumentbeskrivelse> dokbliste = new List<dokumentbeskrivelse>();

                if (input.nyttNotat.hoveddokument != null)
                {
                    var dokbesk = new dokumentbeskrivelse
                    {
                        dokumentstatus = "F",
                        tilknyttetRegistreringSom = "H",
                        tittel = input.nyttNotat.hoveddokument.tittel
                    };

                    if (input.nyttNotat.hoveddokument.skjermetDokument)
                    {
                        dokbesk.skjerming = new skjerming()
                        {
                            //skjermingshjemmel = input.nyttNotat.skjerming?.skjermingshjemmel,
                            skjermingDokument = "Hele"
                        };
                    }
                    var dok = new dokumentobjekt
                    {
                        referanseDokumentfil = input.nyttNotat.hoveddokument.filnavn
                    };
                    List<dokumentobjekt> dokliste = new List<dokumentobjekt>();
                    dokliste.Add(dok);

                    dokbesk.dokumentobjekt = dokliste.ToArray();

                    dokbliste.Add(dokbesk);
                    antFiler++;
                }
                foreach (var item in input.nyttNotat.vedlegg)
                {
                    var dokbesk = new dokumentbeskrivelse();
                    dokbesk.dokumentstatus = "F";
                    dokbesk.tilknyttetRegistreringSom = "V";
                    dokbesk.tittel = item.tittel;

                    var dok = new dokumentobjekt();
                    dok.referanseDokumentfil = item.filnavn;
                    List<dokumentobjekt> dokliste = new List<dokumentobjekt>();
                    dokliste.Add(dok);

                    dokbesk.dokumentobjekt = dokliste.ToArray();

                    dokbliste.Add(dokbesk);
                    antFiler++;

                }
                journalpst.dokumentbeskrivelse = dokbliste.ToArray();

                //Korrespondanseparter
                List<korrespondansepart> partsListe = new List<korrespondansepart>();
                
                foreach (var internMottaker in input.nyttNotat.internAvsender)
                {
                    korrespondansepart korrpart = InternKorrespondansepartToArkivPart(InternavsenderKode, internMottaker);
                    partsListe.Add(korrpart);
                }

                foreach (var internMottaker in input.nyttNotat.internMottaker)
                {
                    korrespondansepart korrpart = InternKorrespondansepartToArkivPart(InternmottakerKode, internMottaker);
                    partsListe.Add(korrpart);
                }

                journalpst.korrespondansepart = partsListe.ToArray();

                if (input.nyttNotat.referanseEksternNoekkel != null)
                {
                    journalpst.referanseEksternNoekkel = new eksternNoekkel();
                    journalpst.referanseEksternNoekkel.fagsystem = input.nyttNotat.referanseEksternNoekkel.fagsystem;
                    journalpst.referanseEksternNoekkel.noekkel = input.nyttNotat.referanseEksternNoekkel.noekkel;
                }

                List<journalpost> jliste = new List<journalpost>
                {
                    journalpst
                };

                if (mappe != null)
                {
                    var mappeliste = new List<saksmappe>();
                    mappe.Items = jliste.ToArray();
                    mappeliste.Add(mappe);
                    arkivmld.Items = mappeliste.ToArray();
                }
                else
                {
                    arkivmld.Items = jliste.ToArray();
                }

            }
            arkivmld.antallFiler = antFiler;
            arkivmld.system = input.nyttNotat.referanseEksternNoekkel.fagsystem;
            arkivmld.meldingId = input.nyttNotat.referanseEksternNoekkel.noekkel;
            arkivmld.tidspunkt = DateTime.Now;

            return arkivmld;
        }

        private static saksmappe ConvertSaksmappe(Saksmappe input)
        {
            saksmappe mappe = new saksmappe
            {
                saksansvarlig = input.saksansvarlig,
                administrativEnhet = input.administrativEnhet,
                tittel = input.tittel
            };
            if (input.saksaar > 0)
                mappe.saksaar = input.saksaar.ToString();
            if (input.sakssekvensnummer > 0)
                mappe.sakssekvensnummer = input.sakssekvensnummer.ToString();

            if (input.saksdato.HasValue)
            {
                mappe.saksdato = input.saksdato.Value;
                mappe.saksdatoSpecified = true;
            }

            if (input.klasse != null)
            {
                List<klasse> klasser = new List<klasse>(); 
                foreach (var kl in input.klasse)
                {
                    klasser.Add(new klasse() { klassifikasjonssystem = kl.klassifikasjonssystem, klasseID = kl.klasseID, tittel = kl.tittel });
                }
                mappe.klasse = klasser.ToArray();
            }
            if (input.referanseEksternNoekkel != null)
            {
                mappe.referanseEksternNoekkel = new eksternNoekkel();
                mappe.referanseEksternNoekkel.fagsystem = input.referanseEksternNoekkel.fagsystem;
                mappe.referanseEksternNoekkel.noekkel = input.referanseEksternNoekkel.noekkel;
            }

            return mappe;
        }
    }
}

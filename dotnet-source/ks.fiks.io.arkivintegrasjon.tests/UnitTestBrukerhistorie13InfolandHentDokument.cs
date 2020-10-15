using System;
using ks.fiks.io.fagsystem.arkiv.sample.ForenkletArkivering;
using no.ks.fiks.io.arkivmelding.sok;
using NUnit.Framework;

namespace ks.fiks.io.arkivintegrasjon.tests
{
    public class UnitTestBrukerhistorie13InfolandHentDokument
    {
        
        
        /// <summary>
        /// Goal: Fetch saksmappe based on cadastre
        /// Input: Kommunenummer, gårdsnummer and bruksnummer
        /// Expected output: Saksmappe
        /// </summary>
        [Test]
        public void testFinnSaksmappeFraMatrikkel()
        {
            // We want to get the casefolder based on KNR, GNR, and BNR
            
            // Kommunenummer, Gårdsnummer og Bruksnummer, Seksjonsnummer og Festenummer
            int KNR = 1149;
            int GNR = 43;
            int BNR = 271;
           
            
            
            var arkivmeldingsok = new sok
            {
                respons = respons_type.saksmappe,
                meldingId = Guid.NewGuid().ToString(),
                system = "Fagsystem X",
                tidspunkt = DateTime.Now,
                skip = 0,
                take = 100
            };
            
            // PARAMETER DEFINITIONS START
            var knrParam = new parameter
            {
                felt = field_type.sakmatrikkelnummerkommunenummer,
                @operator = operator_type.equal,
                parameterverdier = new parameterverdier
                {
                    Item = new intvalues
                    {
                        value = new[] {KNR}
                    }
                }
            };
            
            var gnrParam = new parameter
            {
                felt = field_type.sakmatrikkelnummergaardsnummer,
                @operator = operator_type.equal,
                parameterverdier = new parameterverdier
                {
                    Item = new intvalues
                    {
                        value = new[] {GNR}
                    }
                }
            };
            
            var bnrParam = new parameter
            {
                felt = field_type.sakmatrikkelnummerbruksnummer,
                @operator = operator_type.equal,
                parameterverdier = new parameterverdier
                {
                    Item = new intvalues
                    {
                        value = new[] {BNR}
                    }
                }
            };
            
            // PARAMETER DEFINITIONS END 

            
            // Create new search with the defined parameters 
            var searchParams = new parameter[] {knrParam, gnrParam, bnrParam};
            arkivmeldingsok.parameter = searchParams;

            var payload = Arkivintegrasjon.Serialize(arkivmeldingsok);
            Assert.Pass();
        }
        
        /// <summary>
        /// Goal: Fetch saksmappe based on extended cadastre information
        /// Input: Kommunenummer, gårdsnummer, bruksnummer, seksjonsnummer and festenummer
        /// Expected output: Saksmappe (tegninger, ferdigattest, reguleringsplaner etc..) 
        /// </summary>
         [Test]
        public void testFinnSaksmappeFraMatrikkelMedSeksjonOgFeste()
        {
            // We want to get the casefolder based on KNR, GNR, and BNR
            
            // Kommunenummer, Gårdsnummer og Bruksnummer, Seksjonsnummer og Festenummer
            int KNR = 1149;
            int GNR = 43;
            int BNR = 271;
            int SNR = 123;
            int FNR = 321;
            
            
            var arkivmeldingsok = new sok
            {
                respons = respons_type.saksmappe,
                meldingId = Guid.NewGuid().ToString(),
                system = "Fagsystem X",
                tidspunkt = DateTime.Now,
                skip = 0,
                take = 100
            };
            
            // PARAMETER DEFINITIONS START
            
            // Kommunenummer
            var knrParam = new parameter
            {
                felt = field_type.sakmatrikkelnummerkommunenummer,
                @operator = operator_type.equal,
                parameterverdier = new parameterverdier
                {
                    Item = new intvalues
                    {
                        value = new[] {KNR}
                    }
                }
            };
            
            //Gårdsnummer
            var gnrParam = new parameter
            {
                felt = field_type.sakmatrikkelnummergaardsnummer,
                @operator = operator_type.equal,
                parameterverdier = new parameterverdier
                {
                    Item = new intvalues
                    {
                        value = new[] {GNR}
                    }
                }
            };
            
            // Bruksnummer
            var bnrParam = new parameter
            {
                felt = field_type.sakmatrikkelnummerbruksnummer,
                @operator = operator_type.equal,
                parameterverdier = new parameterverdier
                {
                    Item = new intvalues
                    {
                        value = new[] {BNR}
                    }
                }
            };
            
            // Seksjonsnummer
             var snrParam = new parameter
            {
                felt = field_type.sakmatrikkelnummerseksjonsnummer,
                @operator = operator_type.equal,
                parameterverdier = new parameterverdier
                {
                    Item = new intvalues
                    {
                        value = new[] {SNR}
                    }
                }
            };
             
             //Festenummer
             var fnrParam = new parameter
             {
                 felt = field_type.sakmatrikkelnummerfestenummer,
                 @operator = operator_type.equal,
                 parameterverdier = new parameterverdier
                 {
                     Item = new intvalues
                     {
                         value = new[] {FNR}
                     }
                 }
             };
             
            
            // PARAMETER DEFINITIONS END 

            
            // Create new search with the defined parameters 
            var searchParams = new parameter[] {knrParam, gnrParam, bnrParam, snrParam, fnrParam};
            arkivmeldingsok.parameter = searchParams;

            var payload = Arkivintegrasjon.Serialize(arkivmeldingsok);
            Assert.Pass();
        }
    }
}
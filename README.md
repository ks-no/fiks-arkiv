# fiks-arkiv

## Om applikasjonen
Dette repositoriet inneholder applikasjoner som kan emulere diverse mottakende systemer fra fiks-io plattformen. 
Brukes f.eks. internt ved testing og utvikling av fiks-protokoll-validator. Applikasjonene er console applikasjoner som kjører i bakgrunnen eller man kan kjøre de vha docker-compose.

Repoet inneholder også client biblioteket ks.fiks.io.arkivintegrasjon.client som publiseres som nuget pakke. 

## Requirements
Konfigurasjon av console applikasjonene krever at man har en Fiks-io konto og integrasjon. 
Id'er og passord for disse puttes inn i appsettings.<miljø>.json. 
Keys og certs puttes i mapper som skal ikke commites til github.
For Windows brukere støttes det å hente importert cert fra operativsystem vha thumbprint.

## Annet
**VIKTIG**: Ikke commit certs og keys til github. 








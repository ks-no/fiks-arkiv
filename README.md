# fiks-arkiv

## Om applikasjonen
Dette repositoriet inneholder applikasjoner som kan emulere diverse mottakende systemer fra fiks-io plattformen. 
Brukes f.eks. internt ved testing og utvikling av fiks-protokoll-validator. Applikasjonene er console applikasjoner som kjører i bakgrunnen eller man kan kjøre de vha docker-compose.

Repoet inneholder også client biblioteket ks.fiks.io.arkivintegrasjon.client som publiseres som nuget pakke.

## Testing i Development miljø
[Man kan kjøre testene i Fiks-protokoll-validator her.](https://forvaltning.fiks.dev.ks.no/fiks-validator/#/)

Konto id man kan benytte:
- Arkivsystem: 760fd7d6-435f-4c1b-97d5-92fbe2f603b0
- Fagsystem arkiv: 4a416cde-2aca-4eef-bec4-efddcee0fcea

## Testing i Test miljø
[Man kan kjøre testene i Fiks-protokoll-validator her.](https://forvaltning.fiks.dev.ks.no/fiks-validator/#/)

Konto id man kan benytte:
- Arkivsystem: 8752e128-0e2b-494c-8fab-8e3577aca13d
- Fagsystem arkiv: 91307c59-0ddb-4212-bede-59f98e0edf77

## Requirements
Konfigurasjon av console applikasjonene krever at man har en Fiks-io konto og integrasjon. 
Id'er og passord for disse puttes inn i appsettings.<miljø>.json. 
Keys og certs puttes i mapper som skal ikke commites til github.
Referanser til disse puttes også inn i appsettings.<miljø>.json
For Windows brukere støttes det å hente importert cert fra operativsystem vha thumbprint.

## Annet
For å kunne kjøre disse simulatorene lokalt sammen med fiks-protokoll-validator så må man også kunne kjøre fiks-io lokalt. 







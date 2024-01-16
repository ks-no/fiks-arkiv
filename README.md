# fiks-arkiv (deprecated)

OBS! Dette repository er ikke lenger oppdatert.

Skjemafiler, dokumentasjon og Wiki er nå flyttet til eget repository [fiks-arkiv-specification](https://github.com/ks-no/fiks-arkiv-specification)

Eksempel applikasjon er erstattet med integrasjonstester i [fiks-arkiv-integration-tests-dotnet](https://github.com/ks-no/fiks-arkiv-integration-tests-dotnet) prosjektet

## Hva er dette?
Dette repositoriet inneholdt dokumentasjon, eksempler og brukerhistorier for **fiks-arkiv**, samt applikasjonene **Arkiv simulator** (arkivsystem.sample) og **Fagsystem simulator** (fagsystem.arkiv.sample) som er kjørbare eksempler på implementasjon av Fiks-Arkiv protokollen med Fiks-IO integrasjon.

Se [README.md](dotnet-source/README.md) under dotnet-source for mer informasjon om eksempel applikasjonene og dette arbeidet.

Se [**wiki**](https://github.com/ks-no/fiks-arkiv/wiki) for dokumentasjon.

Se i mappen [**eksempel**](eksempel) for eksempler på meldinger.  

## Applikasjonene

Applikasjonene er console applikasjoner som kjører i bakgrunnen eller man kan kjøre de vha docker-compose.

Nuget biblioteket som inneholder modeller og xsd'er for Fiks-Arkiv, **KS.Fiks.Arkiv.Models.V1** som brukes er tilgjengelig på NuGet [her.](https://www.nuget.org/packages/KS.Fiks.Arkiv.Models.V1/)


### Arkiv simulator (ks.fiks.io.arkivsystem.sample)
Applikasjonen **arkivsystem.sample** er en "Arkiv simulator" som kjører i dev og test og tar i mot meldinger og svarer med faste meldinger tilbake.
Den cacher arkivmeldinger også for at man skal kunne først "arkivere" f.eks. en journalpost og så oppdatere den etterpå ved en oppdater-melding. 
Caching blir gjort når en melding kommer inn med Fiks-IO headeren 'testSessionId'. 
Følgende meldinger som bruker samme header vil da sjekke i cache og f.eks. oppdatere eller hente data fra cache. 
Dette blir brukt av både Fiks-protokoll-validator og integrasjonstester.

Github Fiks-protokoll-validator [her.](https://github.com/ks-no/fiks-protokoll-validator)

Github Fiks-arkiv-integrasjonstester [her.](https://github.com/ks-no/fiks-arkiv-integration-tests-dotnet)


### Fagsystem simulator (ks.fiks.io.fagsystem.arkiv.sample)
Applikasjonen **fagsystem.arkiv** kjører ikke i noen miljøer da den sender noen faste meldinger ved oppstart.

### Testing i Development miljø


Man kan kjøre testene i testmiljøet for *Fiks-protokoll-validator* [her.](https://forvaltning.fiks.dev.ks.no/fiks-validator/#/)

Konto id man kan benytte:
- Arkivsystem: 760fd7d6-435f-4c1b-97d5-92fbe2f603b0
- Fagsystem arkiv: 4a416cde-2aca-4eef-bec4-efddcee0fcea

### Testing i Test miljø
Man kan kjøre testene i *Fiks-protokoll-validator* [her.](https://forvaltning.fiks.test.ks.no/fiks-validator/#/)

Konto id man kan benytte:
- Arkivsystem: b6062766-2a25-4e1c-ae66-f1256b9c449f
- Fagsystem arkiv: 91307c59-0ddb-4212-bede-59f98e0edf77

### Requirements
Konfigurasjon av console applikasjonene krever at man har en Fiks-io konto og integrasjon. 
Id'er og passord for disse puttes inn i appsettings.<miljø>.json. 
Keys og certs puttes i mapper som skal ikke commites til github.
Referanser til disse puttes også inn i appsettings.<miljø>.json
For Windows brukere støttes det å hente importert cert fra operativsystem vha thumbprint.

### Annet
For å kunne kjøre disse simulatorene lokalt sammen med fiks-protokoll-validator så må man også kunne kjøre fiks-io lokalt. 







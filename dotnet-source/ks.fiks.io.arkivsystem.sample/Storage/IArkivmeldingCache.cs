using System.Collections.Generic;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;

namespace ks.fiks.io.arkivsystem.sample.Storage;

public interface IArkivmeldingCache
{
    void Add(string key, Arkivmelding arkivmelding);
    Arkivmelding GetFirst(string key);
    List<Arkivmelding> GetAll(string key);
    bool HasArkivmeldinger(string key);
    //bool TryGetValue(string key, out Arkivmelding lagretArkvivmelding);
}
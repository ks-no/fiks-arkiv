using System.Collections.Generic;
using System.Linq;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;

namespace ks.fiks.io.arkivsystem.sample.Storage;

public class ArkivmeldingCache : IArkivmeldingCache
{
    private static SizedDictionary<string, List<Arkivmelding>> Cache;
    
    public ArkivmeldingCache()
    {
        Cache = new SizedDictionary<string, List<Arkivmelding>>(100);
    }
    
    public void Add(string key, Arkivmelding arkivmelding)
    {
        if (!Cache.ContainsKey(key))
        {
            Cache.Add(key, new List<Arkivmelding>());
        }

        Cache[key].Add(arkivmelding);
    }

    public Arkivmelding GetFirst(string key)
    {
        return Cache[key].First();
    }
    
    public List<Arkivmelding> GetAll(string key)
    {
        return Cache[key];
    }

    public bool HasArkivmeldinger(string key)
    {
       return Cache.ContainsKey(key);
    }
}
using System.IO;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;

namespace ks.fiks.io.arkivintegrasjon.common.Helpers
{
    public class ArkivmeldingSerializeHelper
    {
        public static string Serialize(object arkivmelding)
        {
            var serializer = new XmlSerializer(arkivmelding.GetType());
            var stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, arkivmelding);
            
            return stringWriter.ToString();
        }

        public static Arkivmelding DeSerialize(string arkivmelding)
        {
            var serializer = new XmlSerializer(typeof(Arkivmelding));
            Arkivmelding arkivmeldingDeserialized;
            using (TextReader reader = new StringReader(arkivmelding))
            {
                arkivmeldingDeserialized = (Arkivmelding) serializer.Deserialize(reader);
            }

            return arkivmeldingDeserialized;
        }
    }
}

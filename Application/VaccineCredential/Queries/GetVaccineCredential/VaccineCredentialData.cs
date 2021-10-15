using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;

namespace Application.VaccineCredential.Queries.GetVaccineCredential
{
    public class CredentialSubject
    {
        public string fhirVersion { get; set; }
        [JsonConverter(typeof(FhirConverter))]
        public Bundle fhirBundle { get; set; }
    }

    public class Vc
    {
        public List<string> type { get; set; }
        public CredentialSubject credentialSubject { get; set; }
    }

    public class Vci
    {
        public string iss { get; set; }
        public long nbf { get; set; }
        public Vc vc { get; set; }
    }

    public class VerifiableCredentials
    {
        public List<string> verifiableCredential { get; set; }
    }


    public class FhirConverter : JsonConverter<Bundle>
    {
        private FhirJsonParser parser = new FhirJsonParser();

        public override void WriteJson(JsonWriter writer, Bundle value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToJson());
        }

        public override Bundle ReadJson(JsonReader reader, Type objectType, Bundle existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            string jsonString = jObject.ToString();
            return parser.Parse<Bundle>(jsonString);
        }
    }
}

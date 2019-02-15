using Newtonsoft.Json;

namespace VolyExternalApiAccess.Darr
{
    public class QualityModel
    {
        [JsonProperty("quality")]
        public Quality Quality { get; set; }

        [JsonProperty("revision")]
        public Revision Revision { get; set; }
    }

    public class Quality
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("resolution")]
        public string Resolution { get; set; }
    }
}

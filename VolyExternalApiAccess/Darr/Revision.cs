using Newtonsoft.Json;

namespace VolyExternalApiAccess.Darr
{
    public class Revision
    {
        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("real")]
        public int Real { get; set; }
    }
}

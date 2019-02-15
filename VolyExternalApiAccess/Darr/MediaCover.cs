using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace VolyExternalApiAccess.Darr
{
    public class MediaCover
    {
        [JsonProperty("coverType")]
        public string CoverType { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}

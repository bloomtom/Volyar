using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace VolyExternalApiAccess.Darr
{
    public class Rating
    {
        [JsonProperty("votes")]
        public int Votes { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }
    }
}

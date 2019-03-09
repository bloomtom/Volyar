using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Volyar.Models
{
    public class ApiIntegration
    {
        /// <summary>
        /// The base url for the api calls.
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// The API integration type (sonarr, radarr).
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// If true, conversions are cancelled if an API integration records can't be retrieved.
        /// </summary>
        public bool CancelIfUnavailable { get; set; }
        /// <summary>
        /// An API key if needed for this API.
        /// </summary>
        public string ApiKey { get; set; }
        /// <summary>
        /// A username if needed for http authentication.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// A password if needed for http authentication.
        /// </summary>
        public string Password { get; set; }
    }
}

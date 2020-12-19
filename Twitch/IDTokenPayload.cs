using System;
using Newtonsoft.Json;

namespace StreamMarkers
{
    public class IDTokenPayload
    {
        [JsonProperty("exp")]
        public int Exp;

        [JsonProperty("iat")]
        public int Iat;

        [JsonProperty("iss")]
        public String Iss;

        [JsonProperty("sub")]
        public String Sub;

        [JsonProperty("azp")]
        public String Azp;

        [JsonProperty("preferred_username")]
        public String PreferredUsername;
    }
}
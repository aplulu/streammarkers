using System.Collections.Generic;
using Newtonsoft.Json;

namespace StreamMarkers.Twitch
{
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken;

        [JsonProperty("refresh_token")]
        public string RefreshToken;

        [JsonProperty("expires_in")]
        public int ExpiresIn;

        [JsonProperty("scope")]
        public List<string> Scope;

        [JsonProperty("id_token")]
        public string IDToken;

        [JsonProperty("token_type")]
        public string TokenType;
    }
}
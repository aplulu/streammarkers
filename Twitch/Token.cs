using System;
using System.Text;
using Newtonsoft.Json;
using StreamMarkers.Twitch;

namespace StreamMarkers
{
    public class Token
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        private string _idToken = null;
        public string IDToken
        {
            get => _idToken;
            set
            {
                var parts = value.Split('.');
                if (parts.Length != 3)
                {
                    throw new Exception("Invalid ID Token");
                }

                var encoded = parts[1];
                var pd = encoded.Length % 4;
                if (pd > 0)
                {
                    encoded += new string('=', 4 - pd);
                }
                var b = Convert.FromBase64String(encoded);
                var payloadText = Encoding.UTF8.GetString(b);
                var payload = JsonConvert.DeserializeObject<IDTokenPayload>(payloadText);
                _idTokenPayload = payload;
                _idToken = value;
            }
        }

        private IDTokenPayload _idTokenPayload;
        public DateTime ExpiresAt { get; set; }

        public Token(string accessToken, string refreshToken, string idToken, DateTime expiresAt)
        {
            this.AccessToken = accessToken;
            this.RefreshToken = refreshToken;
            this.IDToken = idToken;
            this.ExpiresAt = expiresAt;
        }

        public bool IsValid()
        {
            return AccessToken != "" && IDToken != "" && _idTokenPayload != null;
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow.AddSeconds(60).CompareTo(ExpiresAt) != -1;
        }

        public String GetUsername()
        {
            return _idTokenPayload.PreferredUsername;
        }

        public String GetSub()
        {
            return _idTokenPayload.Sub;
        }
        
        public Token RefreshFromResponse(TokenResponse resp)
        {
            var expires = DateTime.UtcNow.AddSeconds(resp.ExpiresIn);
            this.ExpiresAt = expires;
            this.AccessToken = resp.AccessToken;
            this.RefreshToken = resp.RefreshToken;
            return this;
        }

        public static Token FromResponse(TokenResponse resp)
        {
            var expires = DateTime.UtcNow.AddSeconds(resp.ExpiresIn);
            return new Token(resp.AccessToken, resp.RefreshToken, resp.IDToken, expires);
        }
        
    }
}
using System;
using System.Collections.Generic;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;

namespace StreamMarkers
{
    public class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        

        public class TwitchTokenObject
        {
            public string AccessToken { get; set; } = "";
            public string RefreshToken { get; set; } = "";
            public string IDToken { get; set; } = "";
            public long ExpiresAt { get; set; } = 0;
        }

        private TwitchTokenObject _twitchToken = null;
        public TwitchTokenObject TwitchToken
        {
            get => _twitchToken;
            set
            {
                _twitchToken = value;
                if (value != null)
                {
                    _token = new Token(value.AccessToken, value.RefreshToken, value.IDToken,
                        DateTime.FromBinary(value.ExpiresAt));
                }
                else
                {
                    _token = null;
                }
            }
        }

        private Token _token = null;

        public Token GetToken()
        {
            return _token;
        }

        public void SetToken(Token token)
        {
            _token = token;
            if (token != null)
            {
                var twitchToken = new TwitchTokenObject();
                twitchToken.AccessToken = token.AccessToken;
                twitchToken.RefreshToken = token.RefreshToken;
                twitchToken.IDToken = token.IDToken;
                twitchToken.ExpiresAt = token.ExpiresAt.ToBinary();
                _twitchToken = twitchToken;
            }
            else
            {
                _twitchToken = null;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using IPALogger = IPA.Logging.Logger;

namespace StreamMarkers.Twitch
{
    public class TwitchAPI
    {
        private static readonly string BASE_URL = "https://api.twitch.tv";
        private static readonly string OAUTH_URL = "https://id.twitch.tv/oauth2";

        public static event Action<Token> TokenRefreshed;

        public static string GetAuthorizeUrl()
        {
            return String.Format(
                "{0}/authorize?client_id={1}&redirect_uri=http://{2}/callback&response_type=code&scope=openid%20channel:manage:broadcast&claims={3}",
                OAUTH_URL, Constants.CLIENT_ID, WebServer.ListenAddress, "{\"id_token\":{\"preferred_username\":null}");
        }

        /**
         * アクセストークンが期限切れの場合は更新
         */
        private static async Task RefreshTokenIfNeeded(Token token)
        {
            if (token.IsExpired())
            {
                await RefreshToken(token);
                TokenRefreshed?.Invoke(token);
            }
        }
        
        /**
         * ストリームマーカーの作成
         */
        public static async Task CreateStreamMarkers(Token token, string description)
        {
            await RefreshTokenIfNeeded(token);
            
            if (description.Length > 140)
            {
                description = description.Substring(0, 137) + "...";
            }

            var client = new HttpClient();
            try
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

                object data = new
                {
                    user_id = token.GetSub(),
                    description = description
                };

                var content = JsonConvert.SerializeObject(data);
                var sc = new StringContent(content);

                sc.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                sc.Headers.Add("Client-Id", Constants.CLIENT_ID);

                var resp = await client.PostAsync(BASE_URL + "/helix/streams/markers", sc).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    throw new TwitchAPIException($"Server returned status code={resp.StatusCode}");
                }
                var respContent = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            finally
            {
                client.Dispose();
            }
        }

        /**
         * トークンを取得
         */
        public static async Task<Token> ExchangeToken(string code)
        {
            var data = new List<KeyValuePair<string, string>>();
            data.Add(new KeyValuePair<string, string>("client_id", Constants.CLIENT_ID));
            data.Add(new KeyValuePair<string, string>("client_secret", Constants.CLIENT_SECRET));
            data.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
            data.Add(new KeyValuePair<string, string>("redirect_uri", String.Format("http://{0}/callback", WebServer.ListenAddress)));
            data.Add(new KeyValuePair<string, string>("code", code));
            
            var fc = new FormUrlEncodedContent(data);

            var client = new HttpClient();
            try
            {
                var resp = await client.PostAsync(OAUTH_URL + "/token", fc).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    throw new TwitchAPIException($"Server returned status code={resp.StatusCode}");
                }
                
                var respContent = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            
                var respToken = JsonConvert.DeserializeObject<TokenResponse>(respContent);

                var token = Token.FromResponse(respToken);
                TokenRefreshed?.Invoke(token);
                
                return token;
            }
            finally
            {
                client.Dispose();
            }
        }

        /**
         * トークンを更新
         */
        public static async Task<Token> RefreshToken(Token token)
        {
            Plugin.Log(IPALogger.Level.Info,"Refreshing token");
            var data = new List<KeyValuePair<string, string>>();
            data.Add(new KeyValuePair<string, string>("client_id", Constants.CLIENT_ID));
            data.Add(new KeyValuePair<string, string>("client_secret", Constants.CLIENT_SECRET));
            data.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
            data.Add(new KeyValuePair<string, string>("refresh_token", token.RefreshToken));
            
            var fc = new FormUrlEncodedContent(data);

            var client = new HttpClient();
            try
            {
                var resp = await client.PostAsync(OAUTH_URL + "/token", fc).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    throw new TwitchAPIException($"Server returned status code={resp.StatusCode}");
                }
                var respContent = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            
                var respToken = JsonConvert.DeserializeObject<TokenResponse>(respContent);
                token.RefreshFromResponse(respToken);
            
                return token;
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}
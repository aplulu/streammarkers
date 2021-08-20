using System;
using System.Threading;
using StreamMarkers.Twitch;
using Zenject;

namespace StreamMarkers.Managers
{
    public class CredentialProvider: IInitializable, IDisposable
    {
        private readonly PluginConfig _pluginConfig;
        private readonly SynchronizationContext _context;
        
        public event Action<Token> TokenRefreshed;
        
        public CredentialProvider(PluginConfig pluginConfig)
        {
            _context = SynchronizationContext.Current;
            _pluginConfig = pluginConfig;
        }

        public void Initialize()
        {
            TwitchAPI.TokenRefreshed += OnTokenRefreshed;
        }

        public void Dispose()
        {
            TwitchAPI.TokenRefreshed -= OnTokenRefreshed;
        }
        
        private void OnTokenRefreshed(Token token)
        {
            _context.Post((state) =>
            {
                _pluginConfig.SetToken(token);
                TokenRefreshed?.Invoke(token);
            }, null);
        }
        
        public Token GetToken()
        {
            return _pluginConfig.GetToken();
        }

        public void SetToken(Token token)
        {
            _pluginConfig.SetToken(token);
            TokenRefreshed?.Invoke(token);
        }
    }
}
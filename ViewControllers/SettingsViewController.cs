using System;
using System.Diagnostics;
using System.Reflection;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using StreamMarkers.Managers;
using Zenject;

namespace StreamMarkers.ViewControllers
{
    public class SettingsViewController: BSMLViewController, IInitializable, IDisposable
    {
        public override string Content => BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "StreamMarkers.Views.Settings.bsml");
        private CredentialProvider _credentialProvider = null;

        [Inject]
        public void Construct(CredentialProvider credentialProvider)
        {
            _credentialProvider = credentialProvider;
        }
        
        public void Initialize()
        {
            _credentialProvider.TokenRefreshed += OnTokenRefreshed;
            BSMLSettings.instance.AddSettingsMenu("StreamMarkers", "StreamMarkers.Views.Settings.bsml", this);
        }

        public void Dispose()
        {
            _credentialProvider.TokenRefreshed -= OnTokenRefreshed;
            BSMLSettings.instance.RemoveSettingsMenu(this);
        }

        private void OnTokenRefreshed(Token token)
        {
            UpdateLoginState();
        }
        
        [UIAction("login-click")]
        private void OnLoginClick()
        {
            Process.Start(Twitch.TwitchAPI.GetAuthorizeUrl());
        }
        
        [UIAction("logout-click")]
        private void OnLogoutClick()
        {
            PluginConfig.Instance.SetToken(null);
            UpdateLoginState();
        }

        [UIValue("is-logged")]
        public bool isLogged
        {
            get
            {
                var token = PluginConfig.Instance.GetToken();
                return token != null && token.IsValid();
            }
        }
        
        [UIValue("is-not-logged")]
        public bool isNotLogged
        {
            get => !isLogged;
        }

        [UIValue("logged-user")]
        public String loggedUser
        {
            get
            {
                var token = PluginConfig.Instance.GetToken();
                return token != null && token.IsValid() ? token.GetUsername() : "not logged";
            }
        }
        
        private void UpdateLoginState()
        {
            NotifyPropertyChanged("isLogged");
            NotifyPropertyChanged("isNotLogged");
            NotifyPropertyChanged("loggedUser");
        }
    }
}
using System;
using System.Diagnostics;
using System.Reflection;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

namespace StreamMarkers.ViewControllers
{
    public class SettingsViewController : BSMLViewController
    {
        public override string Content => BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "StreamMarkers.Views.Settings.bsml");
        
        [UIAction("login-click")]
        private void OnLoginClick()
        {
            Process.Start(Twitch.TwitchAPI.GetAuthorizeUrl());
        }
        
        [UIAction("logout-click")]
        private void OnLogoutClick()
        {
            Plugin.Log("logout clicked");
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
        public void UpdateLoginState()
        {
            NotifyPropertyChanged("isLogged");
            NotifyPropertyChanged("isNotLogged");
            NotifyPropertyChanged("loggedUser");
        }
    }
}
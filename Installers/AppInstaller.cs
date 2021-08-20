using StreamMarkers.Managers;
using StreamMarkers.Twitch;
using Zenject;

namespace StreamMarkers.Installers
{
    public class AppInstaller: Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<CredentialProvider>().AsSingle();
            Container.BindInterfacesAndSelfTo<WebServer>().AsSingle();
        }
    }
}
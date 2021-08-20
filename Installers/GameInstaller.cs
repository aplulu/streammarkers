using StreamMarkers.Managers;
using Zenject;

namespace StreamMarkers.Installers
{
    public class GameInstaller: Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<Marker>().AsSingle();
        }
    }
}
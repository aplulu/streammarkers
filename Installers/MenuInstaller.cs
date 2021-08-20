using SiraUtil;
using StreamMarkers.ViewControllers;
using Zenject;

namespace StreamMarkers.Installers
{
    public class MenuInstaller: Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<SettingsViewController>().FromNewComponentAsViewController().AsSingle();
        }
    }
}
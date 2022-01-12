using IPA;
using IPALogger = IPA.Logging.Logger;
using System.IO;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Loader;
using SiraUtil;
using SiraUtil.Zenject;


namespace StreamMarkers
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        public static string Name => "StreamMarkers";
        public static IPALogger Logger { get; internal set; }

        [Init]
        public Plugin(IPA.Logging.Logger logger, IPA.Config.Config config, Zenjector injector, PluginMetadata metadata)
        {
            Logger = logger;
            var conf = config.Generated<PluginConfig>();
            PluginConfig.Instance = conf;

            injector.UseLogger(logger);
            injector.Install(Location.App, container =>
            {
                container.BindInstance(conf).AsSingle();
            });
            injector.Install<Installers.AppInstaller>(Location.App);
            injector.Install<Installers.MenuInstaller>(Location.Menu);
            injector.Install<Installers.GameInstaller>(Location.Player);
        }
        
        public static void Log(IPALogger.Level level,
            string text,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            Logger.Log(level, $"[{Name}] {Path.GetFileName(file)}->{member}({line}): {text}");
        }
    }
}
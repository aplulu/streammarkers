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

            injector.On<PCAppInit>().Pseudo(container =>
            {
                container.BindLoggerAsSiraLogger(logger);
                container.BindInstance(conf).AsSingle();
            });
            injector.OnApp<Installers.AppInstaller>();
            injector.OnMenu<Installers.MenuInstaller>();
            injector.OnGame<Installers.GameInstaller>();
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
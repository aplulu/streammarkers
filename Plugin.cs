using System;
using IPA;
using IPALogger = IPA.Logging.Logger;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Settings;
using IPA.Config.Stores;
using IPA.Loader;
using StreamMarkers.Twitch;
using StreamMarkers.ViewControllers;
using UnityEngine.SceneManagement;


namespace StreamMarkers
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public static string Name => "StreamMarkers";
        public static SemVer.Version Version => IPA.Loader.PluginManager.GetPluginFromId("StreamMarkers").Version;

        public static IPALogger Logger { get; internal set; }
        
        public static Plugin Instance { get; private set; }

        private SynchronizationContext _context;

        public SettingsViewController SettingsViewController { get; set; }
        
        [Init]
        public Plugin(IPA.Logging.Logger logger, IPA.Config.Config config, PluginMetadata metadata)
        {
            Logger = logger;
            logger.Debug(nameof (Plugin));
            PluginConfig.Instance = config.Generated<PluginConfig>();
        }
        
        public static void Log(IPALogger.Level level,
            string text,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            Logger.Log(level, $"[{Name}] {Path.GetFileName(file)}->{member}({line}): {text}");
        }

        [OnStart]
        public void OnStart()
        {
            if (Instance != null) return;
            Instance = this;

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            BS_Utils.Utilities.BSEvents.earlyMenuSceneLoadedFresh += OnEarlyMenuSceneLoadedFresh;
            TwitchAPI.TokenRefreshed += OnTokenRefreshed;
            
            _context = SynchronizationContext.Current;
            WebServer.Start(_context);
        }

        private void OnEarlyMenuSceneLoadedFresh(ScenesTransitionSetupDataSO obj)
        {
            SettingsViewController = BeatSaberUI.CreateViewController<SettingsViewController>();
            BSMLSettings.instance.AddSettingsMenu(Name, "StreamMarkers.Views.Settings.bsml", SettingsViewController);
        }

        [OnExit]
        public void OnExit()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            BS_Utils.Utilities.BSEvents.earlyMenuSceneLoadedFresh -= OnEarlyMenuSceneLoadedFresh;
            TwitchAPI.TokenRefreshed -= OnTokenRefreshed;

            WebServer.Stop();
        }


        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            if (newScene.name.Equals("GameCore"))
            {
                var context = SynchronizationContext.Current;
                Task.Run(() =>
                {
                    var token = PluginConfig.Instance.GetToken();
                    if (token == null || !token.IsValid())
                    {
                        Log(IPALogger.Level.Warning, "Access Token is not set or not valid.");
                        return;
                    }

                    try
                    {
                        var setupData = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData;
                        var level = setupData.difficultyBeatmap.level;

                        var description = level.songName + " - " + level.songAuthorName;

                        TwitchAPI.CreateStreamMarkers(token, description).Wait();
                    }
                    catch (AggregateException e)
                    {
                        foreach (var inner in e.InnerExceptions)
                        {
                            Log(IPALogger.Level.Error, $"failed to create StreamMarker: {inner.Message}");
                        }
                    }
                    catch (Exception e)
                    {
                        Log(IPALogger.Level.Error, $"failed to create StreamMarker: {e.Message}");
                    }
                });
            }
        }
        
        private void OnTokenRefreshed(Token token)
        {
            _context.Post((state) =>
            {
                PluginConfig.Instance.SetToken(token);
                Plugin.Instance.SettingsViewController.UpdateLoginState();
            }, null);
        }
    }
}
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

        public SettingsViewController SettingsViewController { get; set; }
        
        [Init]
        public Plugin(IPA.Logging.Logger logger, IPA.Config.Config config, PluginMetadata metadata)
        {
            Logger = logger;
            logger.Debug(nameof (Plugin));
            PluginConfig.Instance = config.Generated<PluginConfig>();
        }
        
        public static void Log(string text,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            Logger.Info($"[{Name}] {Path.GetFileName(file)}->{member}({line}): {text}");
        }

        [OnStart]
        public void OnStart()
        {
            if (Instance != null) return;
            Instance = this;

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            BS_Utils.Utilities.BSEvents.earlyMenuSceneLoadedFresh += OnEarlyMenuSceneLoadedFresh;
            
            var context = SynchronizationContext.Current;
            WebServer.Start(context);
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
            
            WebServer.Stop();
        }


        public void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            if (newScene.name.Equals("GameCore"))
            {
                var context = SynchronizationContext.Current;
                Task.Run(() =>
                {
                    var token = PluginConfig.Instance.GetToken();
                    if (token == null || !token.IsValid())
                    {
                        Log("Access Token is not set or not valid.");
                        return;
                    }

                    try
                    {
                        // アクセストークン更新
                        var refreshed = TwitchAPI.RefreshTokenIfNeeded(token);
                        if (refreshed.Result)
                        {
                            Log("Token refreshed");
                            context.Post((state) => { PluginConfig.Instance.SetToken(token); }, null);
                        }

                        var setupData = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData;
                        var level = setupData.difficultyBeatmap.level;

                        var description = level.songName + " - " + level.songAuthorName;

                        TwitchAPI.CreateStreamMarkers(token, description).Wait();
                    }
                    catch (AggregateException e)
                    {
                        foreach (var inner in e.InnerExceptions)
                        {
                            Log($"failed to create StreamMarker: {inner.Message}");
                        }
                    }
                    catch (Exception e)
                    {
                        Log($"failed to create StreamMarker: {e.Message}");
                    }
                });
            }
        }
    }
}
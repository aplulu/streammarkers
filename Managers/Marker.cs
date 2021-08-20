using System;
using System.Threading;
using System.Threading.Tasks;
using SiraUtil.Tools;
using StreamMarkers.Twitch;
using Zenject;

namespace StreamMarkers.Managers
{
    public class Marker: IInitializable
    {
        private readonly SiraLog _log;
        private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;
        private readonly CredentialProvider _credentialProvider;

        public Marker(SiraLog log, GameplayCoreSceneSetupData gameplayCoreSceneSetupData, CredentialProvider credentialProvider)
        {
            _log = log;
            _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
            _credentialProvider = credentialProvider;
        }

        public void Initialize()
        {
            _log.Logger.Info("Initialize");
            var context = SynchronizationContext.Current;
            Task.Run(async () =>
            {
                var token = _credentialProvider.GetToken();
                if (token == null || !token.IsValid())
                {
                    _log.Logger.Warn("Access Token is not set or not valid.");
                }

                try
                {
                    var level = _gameplayCoreSceneSetupData.difficultyBeatmap.level;

                    var description = level.songName + " - " + level.songAuthorName;

                    await TwitchAPI.CreateStreamMarkers(token, description).ConfigureAwait(false);
                    _log.Logger.Info("Marker successfully created. [" + description + "]");
                }
                catch (AggregateException e)
                {
                    foreach (var inner in e.InnerExceptions)
                    {
                        _log.Logger.Error($"failed to create StreamMarker: {inner.Message}");
                    }
                }
                catch (Exception e)
                {
                    _log.Logger.Error( $"failed to create StreamMarker: {e.Message}");
                }
            });
        }
    }
}
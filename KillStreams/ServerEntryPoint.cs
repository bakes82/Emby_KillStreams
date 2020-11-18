using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;

namespace KillStreams
{
    public class ServerEntryPoint : IServerEntryPoint
    {

        // ReSharper disable once TooManyDependencies
        public ServerEntryPoint(ISessionManager sessionManager, ILogManager logManager)
        {
            Instance = this;
            SessionManager = sessionManager;
            LogManager = logManager;
            Logger = LogManager.GetLogger(Plugin.Instance.Name);
        }

        private ILogger Logger { get; }
        private ILogManager LogManager { get; }
        private static ServerEntryPoint Instance { get; set; }
        private static ISessionManager SessionManager { get; set; }



        public void Dispose()
        {
            SessionManager.PlaybackStopped -= PlaybackStopped;
            SessionManager.PlaybackProgress -= PlaybackProgress;
        }

        public void Run()
        {
            SessionManager.PlaybackStopped += PlaybackStopped;
            SessionManager.PlaybackProgress += PlaybackProgress;
        }

        private void PlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            switch (e.Session.PlayState.IsPaused)
            {
                case true:
                    PausedSessionsHelper.AddSessionToList(e.Session.Id);
                    break;
                case false:
                    PausedSessionsHelper.RemoteSessionFromList(e.Session.Id);
                    break;
            }
        }

        private void PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            if (e.IsPaused) return;

            //The item was in a paused state when the user stopped it, clean up the paused session list.
            PausedSessionsHelper.RemoteSessionFromList(e.Session.Id);
        }
    }
}
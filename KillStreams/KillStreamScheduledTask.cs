using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Tasks;

namespace KillStreams
{
    public class KillStreamScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        public KillStreamScheduledTask(ISessionManager sessionManager, ILogger logger, IUserManager userManager)
        {
            SessionManager = sessionManager;
            Logger = logger;
            UserManager = userManager;
        }

        private ISessionManager SessionManager { get; }
        private ILogger Logger { get; }
        private IUserManager UserManager { get; }
        public bool IsHidden => false;
        public bool IsEnabled => true;
        public bool IsLogged => true;

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var rnd = new Random();
            var rndMaster = rnd.Next(1, 10);
            Logger.Info($"Count of streams {SessionManager.Sessions.Count()}");
            Logger.Info(
                $"AllowAudioTranscode {Plugin.Instance.PluginConfiguration.Allow4KAudioTranscode} AllowVideoTranscode {Plugin.Instance.PluginConfiguration.Allow4KVideoTranscode}");
            foreach (var sessionManagerSession in SessionManager.Sessions)
            {
                Logger.Info(
                    $"Device Id {sessionManagerSession.DeviceId} - UserName {sessionManagerSession.UserName} - ID {sessionManagerSession.Id} PlayState Method {sessionManagerSession.PlayState?.PlayMethod} AudioDirect {sessionManagerSession.TranscodingInfo?.IsAudioDirect} Video Direct {sessionManagerSession.TranscodingInfo?.IsVideoDirect}");
                if (sessionManagerSession.PlayState != null &&
                    sessionManagerSession.PlayState.PlayMethod == PlayMethod.Transcode &&
                    sessionManagerSession.NowPlayingItem != null)
                {
                    var mediaSourceItem =
                        sessionManagerSession.FullNowPlayingItem.GetMediaSources(false, false, new LibraryOptions())
                            .Single(x =>
                                string.Equals(x.Id, sessionManagerSession.PlayState.MediaSourceId,
                                    StringComparison.CurrentCultureIgnoreCase));

                    Logger.Info(
                        $"Height {mediaSourceItem.VideoStream.Height} Width  {mediaSourceItem.VideoStream.Width}");
                    var is4K = mediaSourceItem.VideoStream.Height <= 2160 &&
                               mediaSourceItem.VideoStream.Width <= 4096 &&
                               mediaSourceItem.VideoStream.Height > 1080 &&
                               mediaSourceItem.VideoStream.Width > 1920;

                    if (!is4K) is4K = mediaSourceItem.VideoStream.DisplayTitle.ToLower().Contains("4k");

                    if (sessionManagerSession.TranscodingInfo != null && is4K &&
                        !Plugin.Instance.PluginConfiguration.Allow4KVideoTranscode &&
                        !sessionManagerSession.TranscodingInfo.IsVideoDirect)
                    {
                        Logger.Info("Inside Kill Video 4k");
                        Logger.Info(
                            $"Device Id {sessionManagerSession.DeviceId} - UserName {sessionManagerSession.UserName} - ID {sessionManagerSession.Id}");

                        await SessionManager.SendPlaystateCommand(null, sessionManagerSession.Id,
                            new PlaystateRequest
                            {
                                Command = PlaystateCommand.Stop,
                                ControllingUserId = UserManager.Users
                                    .FirstOrDefault(user => user.Policy.IsAdministrator)?.Id.ToString()
                            }, new CancellationToken());

                        var text = "Stream stopped because of transcoding.  Reason(s): " + string.Join(", ",
                                       sessionManagerSession.TranscodingInfo.TranscodeReasons) +
                                   "Try adjusting your video internet quality settings to a higher value and/or changing audio sources depending on your reason.";

                        await SessionManager.SendMessageCommand(null, sessionManagerSession.Id,
                            new MessageCommand
                            {
                                Header = "4K Stream Video Transcoding Disabled",
                                Text = prettyText(text)
                                //TimeoutMs = 10000
                            },
                            new CancellationToken());
                    }

                    if (sessionManagerSession.TranscodingInfo != null && is4K &&
                        !Plugin.Instance.PluginConfiguration.Allow4KAudioTranscode &&
                        !sessionManagerSession.TranscodingInfo.IsAudioDirect)
                    {
                        Logger.Info("Inside Kill Audio 4k");
                        Logger.Info(
                            $"Device Id {sessionManagerSession.DeviceId} - UserName {sessionManagerSession.UserName} - ID {sessionManagerSession.Id}");

                        await SessionManager.SendPlaystateCommand(null, sessionManagerSession.Id,
                            new PlaystateRequest
                            {
                                Command = PlaystateCommand.Stop,
                                ControllingUserId = UserManager.Users
                                    .FirstOrDefault(user => user.Policy.IsAdministrator)?.Id.ToString()
                            }, new CancellationToken());

                        var text = "Stream stopped because of transcoding.  Reason(s): " + string.Join(", ",
                                       sessionManagerSession.TranscodingInfo.TranscodeReasons) +
                                   "Try adjusting your video internet quality settings to a higher value and/or changing audio sources depending on your reason.";

                        await SessionManager.SendMessageCommand(null, sessionManagerSession.Id,
                            new MessageCommand
                            {
                                Header = "4K Stream Audio Transcoding Disabled",
                                Text = prettyText(text)
                                //TimeoutMs = 10000
                            },
                            new CancellationToken());
                    }

                    if (is4K && Plugin.Instance.PluginConfiguration.Allow4KAudioTranscode &&
                        sessionManagerSession.TranscodingInfo != null &&
                        !sessionManagerSession.TranscodingInfo.IsAudioDirect)
                    {
                        Logger.Info("Inside Allow 4k audio transcode");
                        Logger.Info(
                            $"Device Id {sessionManagerSession.DeviceId} - UserName {sessionManagerSession.UserName} - ID {sessionManagerSession.Id}");
                        var text = "Transcoding.  Reason(s): " + string.Join(", ",
                                       sessionManagerSession.TranscodingInfo.TranscodeReasons) +
                                   "Try changing the audio to match your setup and save the CPU.  However you can continue to play with a audio transcode.";

                        if (sessionManagerSession.PlayState.PositionTicks <=
                            sessionManagerSession.NowPlayingItem.RunTimeTicks * .05)
                            await SessionManager.SendMessageCommand(null, sessionManagerSession.Id,
                                new MessageCommand
                                {
                                    Header = "4K Stream Audio Transcoding Enabled",
                                    Text = prettyText(text)
                                    //TimeoutMs = 10000
                                },
                                new CancellationToken());
                    }
                }

                if (sessionManagerSession.TranscodingInfo != null && sessionManagerSession.PlayState != null &&
                    sessionManagerSession.PlayState.PlayMethod == PlayMethod.Transcode &&
                    !sessionManagerSession.TranscodingInfo.IsVideoDirect &&
                    Plugin.Instance.PluginConfiguration.NagTranscode && rndMaster == rnd.Next(1, 10))
                {
                    Logger.Info("Inside nag transcode");
                    Logger.Info(
                        $"Device Id {sessionManagerSession.DeviceId} - UserName {sessionManagerSession.UserName} - ID {sessionManagerSession.Id}");
                    var text = "You are being nagged for transcoding.  Reason(s): " + string.Join(", ",
                                   sessionManagerSession.TranscodingInfo.TranscodeReasons) +
                               "Try adjusting your video internet quality settings to a higher value and/or changing audio sources depending on your reason.";

                    await SessionManager.SendMessageCommand(null, sessionManagerSession.Id,
                        new MessageCommand
                        {
                            Header = "Stream Transcoding Nag",
                            Text = prettyText(text)
                        },
                        new CancellationToken());
                }
            }

            var killPausedStreams = PausedSessionsHelper.GetSessionsToKill();
            Logger.Info($"Count of paused streams {killPausedStreams.Count()}");

            foreach (var pausedStream in killPausedStreams)
            {
                await SessionManager.SendPlaystateCommand(null, pausedStream.SessionId,
                    new PlaystateRequest
                    {
                        Command = PlaystateCommand.Stop,
                        ControllingUserId = UserManager.Users
                            .FirstOrDefault(user => user.Policy.IsAdministrator)?.Id.ToString()
                    }, new CancellationToken());

                var text = "Stream killed due to being paused for more than " +
                           Plugin.Instance.PluginConfiguration.PausedDurationMin + "min.";

                await SessionManager.SendMessageCommand(null, pausedStream.SessionId,
                    new MessageCommand
                    {
                        Header = "Paused Stream Killed",
                        Text = prettyText(text)
                        //TimeoutMs = 10000
                    },
                    new CancellationToken());
            }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerInterval,
                    IntervalTicks = TimeSpan.FromMinutes(1).Ticks
                }
            };
        }

        public string Name => "Task to kill streams";
        public string Key => "KillStreams";
        public string Description => "Task to kill streams.";
        public string Category => "Kill Streams";

        public string prettyText(string input)
        {
            if (input.Contains("ContainerNotSupported"))
                input = input.Replace("ContainerNotSupported", "Container is not supported.  ");

            if (input.Contains("VideoCodecNotSupported"))
                input = input.Replace("VideoCodecNotSupported", "Video codec is not supported.  ");

            if (input.Contains("AudioCodecNotSupported"))
                input = input.Replace("AudioCodecNotSupported", "Audio codec is not supported.  ");

            if (input.Contains("ContainerBitrateExceedsLimit"))
                input = input.Replace("ContainerBitrateExceedsLimit",
                    "The file is in a higher quality than your selected play back quality.  ");

            if (input.Contains("AudioBitrateNotSupported"))
                input = input.Replace("AudioBitrateNotSupported", "Audio bitrate is not supported.  ");

            if (input.Contains("AudioChannelsNotSupported"))
                input = input.Replace("AudioChannelsNotSupported", "Audio channels is not supported.  ");

            if (input.Contains("VideoResolutionNotSupported"))
                input = input.Replace("VideoResolutionNotSupported", "Video resolution is not supported.  ");

            if (input.Contains("UnknownVideoStreamInfo"))
                input = input.Replace("UnknownVideoStreamInfo", "Unknown video stream info.  ");

            if (input.Contains("UnknownAudioStreamInfo"))
                input = input.Replace("UnknownAudioStreamInfo", "Unknown audio stream info.  ");

            if (input.Contains("AudioProfileNotSupported"))
                input = input.Replace("AudioProfileNotSupported", "Audio profile is not supported.  ");

            if (input.Contains("AudioSampleRateNotSupported"))
                input = input.Replace("AudioSampleRateNotSupported", "Audio sample rate is not supported.  ");

            if (input.Contains("AnamorphicVideoNotSupported"))
                input = input.Replace("AnamorphicVideoNotSupported", "Anamorphic video is not supported.  ");

            if (input.Contains("InterlacedVideoNotSupported"))
                input = input.Replace("InterlacedVideoNotSupported", "Interlaced video is not supported.  ");

            if (input.Contains("SecondaryAudioNotSupported"))
                input = input.Replace("SecondaryAudioNotSupported", "Secondary audio is not supported.  ");

            if (input.Contains("RefFramesNotSupported"))
                input = input.Replace("RefFramesNotSupported", "Ref frames is not supported.  ");

            if (input.Contains("VideoBitDepthNotSupported"))
                input = input.Replace("VideoBitDepthNotSupported", "Video bit depth is not supported.  ");

            if (input.Contains("VideoBitrateNotSupported"))
                input = input.Replace("VideoBitrateNotSupported", "Video bitrate is not supported.  ");

            if (input.Contains("VideoFramerateNotSupported"))
                input = input.Replace("VideoFramerateNotSupported", "Video framerate is not supported.  ");

            if (input.Contains("VideoLevelNotSupported"))
                input = input.Replace("VideoLevelNotSupported", "Video level is not supported.  ");

            if (input.Contains("VideoProfileNotSupported"))
                input = input.Replace("VideoProfileNotSupported", "Video profile is not supported.  ");

            if (input.Contains("AudioBitDepthNotSupported"))
                input = input.Replace("AudioBitDepthNotSupported", "Audio bit depth is not supported.  ");

            if (input.Contains("SubtitleCodecNotSupported"))
                input = input.Replace("SubtitleCodecNotSupported", "Subtitle codec is not supported.  ");

            if (input.Contains("DirectPlayError")) input = input.Replace("DirectPlayError", "Direct play error.  ");

            return input;
        }
    }
}
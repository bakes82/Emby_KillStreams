using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace KillStreams
{
    public class PausedSession
    {
        public string SessionId { get; set; }
        public DateTime PausedAtTimeUtc { get; set; }
        public DateTime KillAtTimeUtc { get; set; }
    }

    public static class PausedSessionsHelper
    {
        private static List<PausedSession> PausedSessions { get; set; }

        public static void AddSessionToList(string sessionId)
        {
            var duration = Plugin.Instance.PluginConfiguration.PausedDurationMin == 0
                ? 5
                : Plugin.Instance.PluginConfiguration.PausedDurationMin;
            
            PausedSessions ??= new List<PausedSession>();

            if(!PausedSessions.Select(x => x.SessionId).Contains(sessionId))
            {
                PausedSessions.Add(new PausedSession{SessionId = sessionId, PausedAtTimeUtc = DateTime.UtcNow, KillAtTimeUtc = DateTime.UtcNow.AddMinutes(duration)});
            }
        }
        
        public static void RemoteSessionFromList(string sessionId)
        {
            PausedSessions ??= new List<PausedSession>();

            PausedSessions.RemoveAll(x => x.SessionId == sessionId);
        }

        public static List<PausedSession> GetSessionsToKill()
        {
            PausedSessions ??= new List<PausedSession>();

            var output = PausedSessions.Where(x => x.KillAtTimeUtc <= DateTime.UtcNow).ToList();

            return output;
        }
    }
}
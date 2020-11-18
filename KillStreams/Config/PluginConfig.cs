using System;
using MediaBrowser.Model.Plugins;

namespace KillStreams
{
    public class PluginConfig : BasePluginConfiguration
    {
        public Guid Guid = new Guid("1880798F-1EB9-40B3-807A-D52F04DA9A88"); // Also Needs Set In HTML File
        public string PluginName => "KillStreams";
        public string PluginDesc => "Kill Streams";
        
        public bool Allow4KVideoTranscode { get; set; }
        public bool Allow4KAudioTranscode { get; set; }
        public bool NagTranscode { get; set; }
        public short PausedDurationMin { get; set; }
    }
}
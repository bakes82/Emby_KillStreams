using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace KillStreams
{
    public class Plugin : BasePlugin<PluginConfig>, IHasThumbImage, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths,
            xmlSerializer)
        {
            Instance = this;
        }
        public static Plugin Instance { get; private set; }

        public override Guid Id => PluginConfiguration.Guid;

        public override string Description => PluginConfiguration.PluginDesc;
        public override string Name => PluginConfiguration.PluginName;
        
        public ImageFormat ThumbImageFormat => ImageFormat.Png;
        public PluginConfig PluginConfiguration => Configuration;

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }
        
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = PluginConfiguration.PluginName,
                    EmbeddedResourcePath = GetType().Namespace + ".Config.configPage.html"
                }
            };
        }
    }
}
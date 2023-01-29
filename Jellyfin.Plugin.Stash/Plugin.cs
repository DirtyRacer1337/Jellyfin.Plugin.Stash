using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Stash.Configuration;

#if __EMBY__
using System.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
#else
using System.Net.Http;
using Microsoft.Extensions.Logging;
#endif

[assembly: CLSCompliant(false)]

namespace Stash
{
#if __EMBY__
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IHttpClient http, ILogManager logger)
#else
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IHttpClientFactory http, ILogger<Plugin> logger)
#endif
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            Http = http;

#if __EMBY__
            if (logger != null)
            {
                Log = logger.GetLogger(this.Name);
            }
#else
            Log = logger;
#endif
        }

#if __EMBY__
        public static IHttpClient Http { get; set; }
#else
        public static IHttpClientFactory Http { get; set; }
#endif

        public static ILogger Log { get; set; }

        public static Plugin Instance { get; private set; }

        public override string Name => "Stash";

        public override Guid Id => Guid.Parse("57b8ef5d-8835-436d-9514-a709ee25faf2");

        public IEnumerable<PluginPageInfo> GetPages()
            => new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name,
                    EmbeddedResourcePath = $"{this.GetType().Namespace}.Configuration.configPage.html",
                },
            };
    }
}

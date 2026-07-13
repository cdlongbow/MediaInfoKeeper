using System.Threading.Tasks;
using Emby.Web.GenericEdit.Elements;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaInfoKeeper.Options.Store;
using MediaInfoKeeper.Options.UIBaseClasses.Views;
using MediaInfoKeeper.Services;

namespace MediaInfoKeeper.Options.View {
    internal class NetWorkPageView : PluginPageView {
        private readonly NetWorkOptionsStore store;

        public NetWorkPageView(PluginInfo pluginInfo, NetWorkOptionsStore store)
            : base(pluginInfo.Id) {
            this.store = store;
            ContentData = store.GetOptions();
        }

        public NetWorkOptions Options => ContentData as NetWorkOptions;

        public override async Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data) {
            var proxyProbeTask = NetworkProbe.RunProxyLatencyAsync(Options);
            var tmdbProbeTask = NetworkProbe.RunTmdbAltAsync(Options);
            await Task.WhenAll(proxyProbeTask, tmdbProbeTask).ConfigureAwait(false);

            var proxyResult = proxyProbeTask.Result;
            Options.ProxyLatencyStatus =
                new StatusItem(proxyResult.Caption, proxyResult.StatusText, proxyResult.Status);
            Options.ShowProxyLatencyStatus = true;

            var tmdbResult = tmdbProbeTask.Result;
            Options.TmdbReplacementStatus =
                new StatusItem(tmdbResult.Caption, tmdbResult.StatusText, tmdbResult.Status);
            Options.ShowTmdbReplacementStatus = true;

            store.SetOptions(Options);
            return await base.OnSaveCommand(itemId, commandId, data).ConfigureAwait(false);
        }
    }
}

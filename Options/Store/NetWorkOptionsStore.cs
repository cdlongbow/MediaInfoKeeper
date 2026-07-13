using Emby.Web.GenericEdit.Elements;

namespace MediaInfoKeeper.Options.Store {
    internal class NetWorkOptionsStore {
        private readonly PluginOptionsStore pluginOptionsStore;

        public NetWorkOptionsStore(PluginOptionsStore pluginOptionsStore) {
            this.pluginOptionsStore = pluginOptionsStore;
        }

        public NetWorkOptions GetOptions() {
            var options = pluginOptionsStore.GetOptionsForUi();
            var networkOptions = options.GetNetWorkOptions();
            networkOptions.ProxyLatencyStatus = new StatusItem();
            networkOptions.ShowProxyLatencyStatus = false;
            networkOptions.TmdbReplacementStatus = new StatusItem();
            networkOptions.ShowTmdbReplacementStatus = false;
            return networkOptions;
        }

        public void SetOptions(NetWorkOptions options) {
            var pluginOptions = pluginOptionsStore.GetOptions();
            pluginOptions.NetWork = options ?? new NetWorkOptions();
            pluginOptionsStore.SetOptions(pluginOptions);
        }
    }
}

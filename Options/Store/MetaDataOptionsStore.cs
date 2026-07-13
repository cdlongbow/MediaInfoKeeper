namespace MediaInfoKeeper.Options.Store {
    internal class MetaDataOptionsStore {
        private readonly PluginOptionsStore pluginOptionsStore;

        public MetaDataOptionsStore(PluginOptionsStore pluginOptionsStore) {
            this.pluginOptionsStore = pluginOptionsStore;
        }

        public MetaDataOptions GetOptions() {
            var options = pluginOptionsStore.GetOptionsForUi();
            return options.MetaData ?? new MetaDataOptions();
        }

        public void SetOptions(MetaDataOptions options) {
            var pluginOptions = pluginOptionsStore.GetOptions();
            pluginOptions.MetaData = options ?? new MetaDataOptions();
            pluginOptionsStore.SetOptions(pluginOptions);
        }
    }
}

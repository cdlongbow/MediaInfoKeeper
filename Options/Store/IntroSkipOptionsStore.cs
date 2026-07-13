namespace MediaInfoKeeper.Options.Store {
    internal class IntroSkipOptionsStore {
        private readonly PluginOptionsStore pluginOptionsStore;

        public IntroSkipOptionsStore(PluginOptionsStore pluginOptionsStore) {
            this.pluginOptionsStore = pluginOptionsStore;
        }

        public IntroSkipOptions GetOptions() {
            var options = pluginOptionsStore.GetOptionsForUi();
            var introSkipOptions = options.IntroSkip ?? new IntroSkipOptions();
            introSkipOptions.Initialize();
            return introSkipOptions;
        }

        public void SetOptions(IntroSkipOptions options) {
            var pluginOptions = pluginOptionsStore.GetOptions();
            pluginOptions.IntroSkip = options ?? new IntroSkipOptions();
            pluginOptionsStore.SetOptions(pluginOptions);
        }
    }
}

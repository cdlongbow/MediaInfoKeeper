namespace MediaInfoKeeper.Options.Store {
    internal class MediaInfoOptionsStore {
        private readonly PluginOptionsStore pluginOptionsStore;

        public MediaInfoOptionsStore(PluginOptionsStore pluginOptionsStore) {
            this.pluginOptionsStore = pluginOptionsStore;
        }

        public MediaInfoOptions GetOptions() {
            var options = pluginOptionsStore.GetOptionsForUi();
            options.MediaInfo ??= new MediaInfoOptions();
            var mediaInfoOptions = options.MediaInfo;
            mediaInfoOptions.Initialize();
            return mediaInfoOptions;
        }

        public void SetOptions(MediaInfoOptions options) {
            var pluginOptions = pluginOptionsStore.GetOptions();
            pluginOptions.MediaInfo = options ?? new MediaInfoOptions();
            pluginOptionsStore.SetOptions(pluginOptions);
        }
    }
}

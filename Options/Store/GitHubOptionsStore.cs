namespace MediaInfoKeeper.Options.Store {
    internal class GitHubOptionsStore {
        private readonly PluginOptionsStore pluginOptionsStore;

        public GitHubOptionsStore(PluginOptionsStore pluginOptionsStore) {
            this.pluginOptionsStore = pluginOptionsStore;
        }

        public GitHubOptions GetOptions() {
            var options = pluginOptionsStore.GetOptionsForUi();
            return options.GitHub ?? new GitHubOptions();
        }

        public void SetOptions(GitHubOptions options) {
            var pluginOptions = pluginOptionsStore.GetOptions();
            pluginOptions.GitHub = options ?? new GitHubOptions();
            pluginOptionsStore.SetOptions(pluginOptions);
        }
    }
}

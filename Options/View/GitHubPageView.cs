using System.Threading.Tasks;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaInfoKeeper.Options.Store;
using MediaInfoKeeper.Options.UIBaseClasses.Views;

namespace MediaInfoKeeper.Options.View {
    internal class GitHubPageView : PluginPageView {
        private readonly GitHubOptionsStore store;

        public GitHubPageView(PluginInfo pluginInfo, GitHubOptionsStore store)
            : base(pluginInfo.Id) {
            this.store = store;
            ContentData = store.GetOptions();
        }

        public GitHubOptions Options => ContentData as GitHubOptions;

        public override Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data) {
            store.SetOptions(Options);
            return base.OnSaveCommand(itemId, commandId, data);
        }
    }
}

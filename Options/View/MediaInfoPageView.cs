using System.Threading.Tasks;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaInfoKeeper.Options.Store;
using MediaInfoKeeper.Options.UIBaseClasses.Views;

namespace MediaInfoKeeper.Options.View {
    internal class MediaInfoPageView : PluginPageView {
        private readonly MediaInfoOptionsStore store;

        public MediaInfoPageView(PluginInfo pluginInfo, MediaInfoOptionsStore store)
            : base(pluginInfo.Id) {
            this.store = store;
            ContentData = store.GetOptions();
        }

        public MediaInfoOptions Options => ContentData as MediaInfoOptions;

        public override Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data) {
            store.SetOptions(Options);
            return base.OnSaveCommand(itemId, commandId, data);
        }
    }
}

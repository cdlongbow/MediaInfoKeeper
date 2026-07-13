using System.Threading.Tasks;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaInfoKeeper.Options.Store;
using MediaInfoKeeper.Options.UIBaseClasses.Views;

namespace MediaInfoKeeper.Options.View {
    internal class MetaDataPageView : PluginPageView {
        private readonly MetaDataOptionsStore store;

        public MetaDataPageView(PluginInfo pluginInfo, MetaDataOptionsStore store)
            : base(pluginInfo.Id) {
            this.store = store;
            ContentData = store.GetOptions();
        }

        public MetaDataOptions Options => ContentData as MetaDataOptions;

        public override Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data) {
            store.SetOptions(Options);
            return base.OnSaveCommand(itemId, commandId, data);
        }
    }
}

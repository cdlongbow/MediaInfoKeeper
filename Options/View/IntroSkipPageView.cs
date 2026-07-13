using System.Threading.Tasks;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaInfoKeeper.Options.Store;
using MediaInfoKeeper.Options.UIBaseClasses.Views;

namespace MediaInfoKeeper.Options.View {
    internal class IntroSkipPageView : PluginPageView {
        private readonly IntroSkipOptionsStore store;

        public IntroSkipPageView(PluginInfo pluginInfo, IntroSkipOptionsStore store)
            : base(pluginInfo.Id) {
            this.store = store;
            ContentData = store.GetOptions();
        }

        public IntroSkipOptions Options => ContentData as IntroSkipOptions;

        public override Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data) {
            store.SetOptions(Options);
            return base.OnSaveCommand(itemId, commandId, data);
        }
    }
}

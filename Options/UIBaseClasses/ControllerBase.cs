using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI;
using MediaBrowser.Model.Plugins.UI.Views;

namespace MediaInfoKeeper.Options.UIBaseClasses {
    internal abstract class ControllerBase : IPluginUIPageController {
        protected ControllerBase(string pluginId) {
            PluginId = pluginId;
        }

        public string PluginId { get; }

        public abstract PluginPageInfo PageInfo { get; }

        public virtual Task Initialize(CancellationToken token) {
            return Task.CompletedTask;
        }

        public abstract Task<IPluginUIView> CreateDefaultPageView();
    }
}

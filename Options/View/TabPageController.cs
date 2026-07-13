using System;
using System.Threading.Tasks;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaInfoKeeper.Options.UIBaseClasses;

namespace MediaInfoKeeper.Options.View {
    internal class TabPageController : ControllerBase {
        private readonly Func<PluginInfo, IPluginUIView> factoryFunc;
        private readonly PluginInfo pluginInfo;

        public TabPageController(PluginInfo pluginInfo, string name, string displayName,
            Func<PluginInfo, IPluginUIView> factoryFunc)
            : base(pluginInfo.Id) {
            this.pluginInfo = pluginInfo;
            this.factoryFunc = factoryFunc;
            PageInfo = new PluginPageInfo {
                Name = name,
                DisplayName = displayName
            };
        }

        public override PluginPageInfo PageInfo { get; }

        public override Task<IPluginUIView> CreateDefaultPageView() {
            var view = factoryFunc(pluginInfo);
            return Task.FromResult(view);
        }
    }
}

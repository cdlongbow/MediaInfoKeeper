using Emby.Web.GenericEdit.Elements;
using MediaInfoKeeper.Services;

namespace MediaInfoKeeper.Options.Store {
    internal class MainPageOptionsStore {
        private readonly PluginOptionsStore pluginOptionsStore;

        public MainPageOptionsStore(PluginOptionsStore pluginOptionsStore) {
            this.pluginOptionsStore = pluginOptionsStore;
        }

        public MainPageOptions GetOptions() {
            var options = pluginOptionsStore.GetOptionsForUi();
            var mainPage = options.MainPage ?? new MainPageOptions();
            mainPage.ScheduledTasksEditor ??= new MainPageOptions.ScheduledTaskEditorOptions();
            mainPage.ShowRefreshQueueStatus = options.Enhance?.TakeOverRefreshQueue ?? true;
            mainPage.RefreshQueueStatus = BuildRefreshQueueStatus();
            return mainPage;
        }

        public void SetOptions(MainPageOptions options) {
            var pluginOptions = pluginOptionsStore.GetOptions();
            pluginOptions.MainPage = options ?? new MainPageOptions();
            pluginOptionsStore.SetOptions(pluginOptions);
        }

        private static StatusItem BuildRefreshQueueStatus() {
            var metadataStats = MetaDataRunner.GetQueueStats();
            var mediaInfoStats = MediaInfoRunner.GetQueueStats();

            return new StatusItem(
                "刷新队列",
                $"提取媒体信息：{mediaInfoStats.Total} / {mediaInfoStats.MaxConcurrent}\n" +
                $"刷新媒体元数据：{metadataStats.Total} / {metadataStats.MaxConcurrent}",
                metadataStats.Running != 0 || mediaInfoStats.Running != 0
                    ? ItemStatus.InProgress
                    : ItemStatus.Succeeded);
        }
    }
}

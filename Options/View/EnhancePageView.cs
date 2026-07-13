using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaInfoKeeper.Options.Store;
using MediaInfoKeeper.Options.UIBaseClasses.Views;
using MediaInfoKeeper.Patch;

namespace MediaInfoKeeper.Options.View {
    internal class EnhancePageView : PluginPageView {
        private const string OptimizeDatabaseCommandId = "enhance.optimizeDatabase";
        private readonly IItemRepository itemRepository;
        private readonly ILibraryManager libraryManager;
        private readonly ILogger logger;
        private readonly EnhanceOptionsStore store;

        public EnhancePageView(PluginInfo pluginInfo, EnhanceOptionsStore store)
            : base(pluginInfo.Id) {
            this.store = store;
            logger = Plugin.Instance.AppHost.Resolve<ILogManager>().GetLogger(Plugin.PluginName);
            libraryManager = Plugin.LibraryManager;
            itemRepository = Plugin.Instance.ItemRepository;
            ContentData = store.GetOptions();
        }

        public EnhanceOptions Options => ContentData as EnhanceOptions;

        public override async Task<IPluginUIView> RunCommand(string itemId, string commandId, string data) {
            if (string.Equals(commandId, OptimizeDatabaseCommandId, StringComparison.Ordinal)) {
                if (Plugin.NotificationApi != null) await Plugin.NotificationApi.DisplayMessage(User, "开始优化数据库").ConfigureAwait(false);

                var message = await Task.Run(() => ExecuteOptimizeDatabase()).ConfigureAwait(false);
                if (Plugin.NotificationApi != null) await Plugin.NotificationApi.DisplayMessage(User, message).ConfigureAwait(false);

                ContentData = store.GetOptions();
                return this;
            }

            return await base.RunCommand(itemId, commandId, data).ConfigureAwait(false);
        }

        public override Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data) {
            store.SetOptions(Options);
            ContentData = store.GetOptions();
            return base.OnSaveCommand(itemId, commandId, data);
        }

        private string ExecuteOptimizeDatabase(CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            logger.Info("开始执行: 优化数据库（重建搜索索引 + 重建排序名 + 清理裂图记录）");

            var rebuilt = ChineseSearch.RebuildSearchIndex();
            if (!rebuilt) throw new InvalidOperationException("重建搜索索引失败，请查看 Emby 系统日志中 EnhanceChineseSearch 相关记录。");

            var updatedItems = 0;
            var removedItemImages = 0;
            var removedChapterImages = 0;
            var rebuiltSortNames = 0;
            var pinyinSortNameEnabled = Plugin.Instance?.Options?.Enhance?.EnablePinyinSortName == true;
            var items = libraryManager.GetItemList(new InternalItemsQuery {
                Recursive = true
            }, cancellationToken);

            foreach (var item in items) {
                cancellationToken.ThrowIfCancellationRequested();

                var currentRemovedItemImages = RemoveBrokenItemImages(item);
                var currentRemovedChapterImages = RemoveBrokenChapterImages(item);
                var sortNameRebuilt = PinyinSortName.RebuildSortName(item);
                if (currentRemovedItemImages == 0 && currentRemovedChapterImages == 0 && !sortNameRebuilt) continue;

                updatedItems++;
                removedItemImages += currentRemovedItemImages;
                removedChapterImages += currentRemovedChapterImages;

                if (sortNameRebuilt) {
                    rebuiltSortNames++;
                    item.UpdateToRepository(ItemUpdateType.MetadataEdit);
                }
            }

            var message =
                $"数据库优化完成：排序名 {rebuiltSortNames}，裂图 {removedItemImages + removedChapterImages}";
            logger.Info(
                $"数据库优化完成: 重建搜索索引 True，排序名规则 {(pinyinSortNameEnabled ? "拼音" : "Emby 原生")}，重建排序名 {rebuiltSortNames} 个，删除条目图片 {removedItemImages} 条，删除章节图片 {removedChapterImages} 条，影响条目 {updatedItems} 个");

            return message;
        }

        private int RemoveBrokenItemImages(BaseItem item) {
            var existingImages = item.ImageInfos ?? Array.Empty<ItemImageInfo>();
            var brokenImages = existingImages
                .Where(i => i != null && IsBrokenPath(i.Path))
                .ToList();

            if (brokenImages.Count == 0) return 0;

            item.RemoveImages(brokenImages);
            itemRepository.SaveImages(item.InternalId, item.ImageInfos ?? Array.Empty<ItemImageInfo>());
            return brokenImages.Count;
        }

        private int RemoveBrokenChapterImages(BaseItem item) {
            var chapters = itemRepository.GetChapters(item) ?? new List<ChapterInfo>();
            var removed = 0;

            foreach (var chapter in chapters) {
                if (!IsBrokenPath(chapter?.ImagePath)) continue;

                chapter.ImagePath = null;
                chapter.ImageTag = null;
                chapter.ImageDateModified = default;
                removed++;
            }

            if (removed == 0) return 0;

            itemRepository.SaveChapters(item.InternalId, chapters);
            return removed;
        }

        private static bool IsBrokenPath(string path) {
            return !string.IsNullOrWhiteSpace(path) &&
                   !IsHttpUrl(path) &&
                   !File.Exists(path);
        }

        private static bool IsHttpUrl(string path) {
            return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }
    }
}

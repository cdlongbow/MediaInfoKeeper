using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;

namespace MediaInfoKeeper.Services {
    /// <summary>
    ///     监听媒体库路径下的新入库 .strm 文件，记录 Created 与 Changed 事件日志。
    /// </summary>
    public sealed class StrmFileWatcher : IDisposable {
        private readonly Dictionary<string, DateTime> createdEvents = new(StringComparer.OrdinalIgnoreCase);

        private readonly TimeSpan directoryReportDedupeWindow = TimeSpan.FromSeconds(2);

        private readonly Dictionary<string, DateTime> lastModifiedEvents = new(StringComparer.OrdinalIgnoreCase);

        private readonly ILibraryManager libraryManager;
        private readonly ILibraryMonitor libraryMonitor;
        private readonly LibraryService libraryService;
        private readonly ILogger logger;
        private readonly TimeSpan modifiedEventDedupeWindow = TimeSpan.FromMilliseconds(100);
        private readonly object syncRoot = new();

        private readonly Dictionary<string, FileSystemWatcher> watchers = new(StringComparer.OrdinalIgnoreCase);

        private volatile bool disposed;

        private volatile bool enabled;

        public StrmFileWatcher(
            ILibraryMonitor libraryMonitor,
            ILibraryManager libraryManager,
            LibraryService libraryService,
            ILogger logger) {
            this.libraryMonitor = libraryMonitor;
            this.libraryManager = libraryManager;
            this.libraryService = libraryService;
            this.logger = logger;
        }

        public void Dispose() {
            if (disposed) return;

            disposed = true;
            enabled = false;

            lock (syncRoot) {
                foreach (var watcher in watchers.Values)
                    try {
                        watcher.EnableRaisingEvents = false;
                        watcher.Dispose();
                    }
                    catch {
                    }

                watchers.Clear();
                createdEvents.Clear();
                lastModifiedEvents.Clear();
            }
        }

        /// <summary>
        ///     配置监听开关。
        /// </summary>
        public void Configure(bool isEnabled, int delaySeconds) {
            if (disposed) return;

            enabled = isEnabled;
            RebuildWatchers(isEnabled);
        }

        /// <summary>
        ///     根据当前配置重建文件监听器。
        /// </summary>
        private void RebuildWatchers(bool isEnabled) {
            lock (syncRoot) {
                foreach (var existing in watchers.Values)
                    try {
                        existing.EnableRaisingEvents = false;
                        existing.Dispose();
                    }
                    catch {
                    }

                watchers.Clear();
                createdEvents.Clear();
                lastModifiedEvents.Clear();

                if (!isEnabled) {
                    logger?.Info("StrmFileWatcher 已禁用");
                    return;
                }

                var roots = (libraryService?.GetAllLibraryPaths() ?? new List<string>())
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(path => path.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var root in roots)
                    try {
                        var watcher = new FileSystemWatcher(root, "*") {
                            IncludeSubdirectories = true,
                            NotifyFilter =
                                NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                            InternalBufferSize = 64 * 1024,
                            EnableRaisingEvents = true
                        };

                        watcher.Created += (sender, args) => OnCreated(args?.FullPath);
                        watcher.Changed += (sender, args) => OnModified(args?.FullPath);
                        watchers[root] = watcher;
                    }
                    catch (Exception ex) {
                        logger?.Warn($"StrmFileWatcher 监听路径失败: {root}");
                        logger?.Warn(ex.Message);
                    }

                logger?.Debug(
                    $"StrmFileWatcher 已启动，监听路径: {string.Join(", ", watchers.Keys.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))}");
            }
        }

        /// <summary>
        ///     记录新增文件事件。
        /// </summary>
        private void OnCreated(string path) {
            if (!IsWatchedMediaFile(path)) return;

            var directoryPath = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directoryPath)) return;

            var shouldReportDirectory = RecordCreatedEvent(directoryPath, path);
            logger?.Info($"新增媒体文件，{Path.GetFileName(path) ?? path}");
            if (!shouldReportDirectory) return;

            try {
                libraryMonitor?.ReportFileSystemChanged(directoryPath);
            }
            catch (Exception ex) {
                logger?.Error($"StrmFileWatcher 通知 Emby 入库扫描失败: {directoryPath}");
                logger?.Error(ex.Message);
            }
        }

        /// <summary>
        ///     记录文件内容修改事件。
        /// </summary>
        private void OnModified(string path) {
            if (!IsWatchedShortcut(path)) return;

            if (ShouldSkipModifiedLog(path)) return;

            logger?.Info($"{Path.GetFileName(path) ?? path} 内容修改");
        }

        private bool IsWatchedShortcut(string path) {
            return enabled &&
                   !disposed &&
                   !string.IsNullOrWhiteSpace(path) &&
                   LibraryService.IsFileShortcut(path);
        }

        private bool IsWatchedMediaFile(string path) {
            return enabled &&
                   !disposed &&
                   !string.IsNullOrWhiteSpace(path) &&
                   (libraryManager.IsVideoFile(path.AsSpan()) ||
                    libraryManager.IsAudioFile(path.AsSpan()));
        }

        private bool RecordCreatedEvent(string directoryPath, string path) {
            var now = DateTime.UtcNow;

            lock (syncRoot) {
                var shouldReportDirectory = !createdEvents.TryGetValue(directoryPath, out var createdAt) ||
                                            now - createdAt >= directoryReportDedupeWindow;
                createdEvents[directoryPath] = now;
                createdEvents[path] = now;
                lastModifiedEvents[path] = now;
                PruneEventCache(createdEvents, now);
                PruneEventCache(lastModifiedEvents, now);
                return shouldReportDirectory;
            }
        }

        private bool ShouldSkipModifiedLog(string path) {
            var now = DateTime.UtcNow;

            lock (syncRoot) {
                if (createdEvents.TryGetValue(path, out var createdAt) &&
                    now - createdAt < modifiedEventDedupeWindow) {
                    lastModifiedEvents[path] = now;
                    PruneEventCache(createdEvents, now);
                    PruneEventCache(lastModifiedEvents, now);
                    return true;
                }

                if (lastModifiedEvents.TryGetValue(path, out var lastSeen) &&
                    now - lastSeen < modifiedEventDedupeWindow)
                    return true;

                lastModifiedEvents[path] = now;
                PruneEventCache(createdEvents, now);
                PruneEventCache(lastModifiedEvents, now);

                return false;
            }
        }

        private void PruneEventCache(Dictionary<string, DateTime> events, DateTime now) {
            var staleBefore = now - modifiedEventDedupeWindow;
            var stalePaths = events
                .Where(pair => pair.Value < staleBefore)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var stalePath in stalePaths) events.Remove(stalePath);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace MediaInfoKeeper.Patch
{
    /// <summary>
    /// 媒体流写入数据库成功后，同步覆盖对应条目的媒体信息 JSON，音频带歌词。
    /// </summary>
    public static class MediaInfoJsonSync
    {
        private static readonly AsyncLocal<bool> SkipPersist = new AsyncLocal<bool>();
        private static Harmony harmony;
        private static ILogger logger;
        private static MethodInfo saveMediaStreams;
        private static bool isEnabled;
        private static bool isPatched;

        public static bool IsReady => harmony != null && (!isEnabled || isPatched);

        public static void Initialize(ILogger pluginLogger, bool enable)
        {
            if (harmony != null)
            {
                Configure(enable);
                return;
            }

            logger = pluginLogger;
            isEnabled = enable;

            try
            {
                var embyServerImplementationsAssembly = Assembly.Load("Emby.Server.Implementations");
                var sqliteItemRepository =
                    embyServerImplementationsAssembly.GetType("Emby.Server.Implementations.Data.SqliteItemRepository");
                var version = embyServerImplementationsAssembly.GetName().Version;
                saveMediaStreams = PatchMethodResolver.Resolve(
                    sqliteItemRepository,
                    version,
                    new MethodSignatureProfile
                    {
                        Name = "sqliteitemrepository-savemediastreams-exact",
                        MethodName = "SaveMediaStreams",
                        BindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                       BindingFlags.NonPublic,
                        ParameterTypes = new[]
                        {
                            typeof(long),
                            typeof(List<MediaStream>),
                            typeof(CancellationToken)
                        }
                    },
                    logger,
                    "MediaInfoJsonSync.SaveMediaStreams");

                if (saveMediaStreams == null)
                {
                    PatchLog.InitFailed(logger, nameof(MediaInfoJsonSync), "目标方法缺失");
                    return;
                }

                harmony = new Harmony("mediainfokeeper.database.mediainfojsonpersist");

                if (isEnabled)
                {
                    Patch();
                }
            }
            catch (Exception e)
            {
                logger?.Error("MediaInfoJsonSync 初始化失败。");
                logger?.Error(e.Message);
                logger?.Error(e.ToString());
                harmony = null;
                isEnabled = false;
            }
        }

        public static void Configure(bool enable)
        {
            isEnabled = enable;

            if (harmony == null)
            {
                return;
            }

            if (isEnabled)
            {
                Patch();
            }
            else
            {
                Unpatch();
            }
        }

        public static IDisposable SkipPersisting()
        {
            var previousValue = SkipPersist.Value;
            SkipPersist.Value = true;
            return new SkipPersistScope(previousValue);
        }

        private static void Patch()
        {
            if (isPatched || harmony == null)
            {
                return;
            }

            harmony.Patch(saveMediaStreams,
                postfix: new HarmonyMethod(typeof(MediaInfoJsonSync), nameof(SaveMediaStreamsPostfix)));
            isPatched = true;
        }

        private static void Unpatch()
        {
            if (!isPatched || harmony == null)
            {
                return;
            }

            harmony.Unpatch(saveMediaStreams, HarmonyPatchType.Postfix, harmony.Id);
            isPatched = false;
        }

        [HarmonyPostfix]
        private static void SaveMediaStreamsPostfix(long itemId, List<MediaStream> streams, bool __runOriginal)
        {
            if (!__runOriginal ||
                streams == null ||
                !streams.Exists(stream =>
                    stream != null &&
                    (stream.Type == MediaStreamType.Video || stream.Type == MediaStreamType.Audio)) ||
                SkipPersist.Value)
            {
                return;
            }

            var item = Plugin.LibraryManager?.GetItemById(itemId);
            if (item == null)
            {
                return;
            }

            try
            {
                item.MediaStreams = streams
                    .Where(stream => stream != null)
                    .ToList();

                Plugin.MediaSourceInfoStore?.OverWriteToFile(item, streams);
                if (item is Audio)
                {
                    Plugin.EmbeddedInfoStore?.OverWriteToFile(item);
                }
            }
            catch (Exception ex)
            {
                logger?.Error($"媒体信息已写入数据库，但同步 JSON 失败: {item.FileName ?? item.Path ?? item.InternalId.ToString()}");
                logger?.Error(ex.Message);
                logger?.Debug(ex.StackTrace);
            }
        }

        private sealed class SkipPersistScope : IDisposable
        {
            private readonly bool previousValue;

            public SkipPersistScope(bool previousValue)
            {
                this.previousValue = previousValue;
            }

            public void Dispose()
            {
                SkipPersist.Value = previousValue;
            }
        }
    }
}

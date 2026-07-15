using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaInfoKeeper.Common;
using MediaInfoKeeper.Patch;
using MediaInfoKeeper.Services;

namespace MediaInfoKeeper.Provider {
    public sealed class IntroDbProvider :
        IRemoteMetadataProvider<Episode, EpisodeInfo>,
        ICustomMetadataProvider<Episode>,
        IHasOrder {
        public const string ProviderName = "IntroDB";
        public const int DefaultOrder = int.MaxValue - 9;

        public async Task<ItemUpdateType> FetchAsync(
            MetadataResult<Episode> itemResult,
            MetadataRefreshOptions options,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken) {
            var item = itemResult?.Item;
            if (!ShouldFetch(itemResult, libraryOptions)) return ItemUpdateType.None;

            var result = await IntroDbService.GetMarkersAsync(item, cancellationToken).ConfigureAwait(false);
            return ApplyMarkers(itemResult, result);
        }

        public int Order => DefaultOrder;

        public Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken) {
            return Task.FromResult(new MetadataResult<Episode> {
                Item = new Episode()
            });
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo,
            CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<RemoteSearchResult>>(Array.Empty<RemoteSearchResult>());
        }

        public string Name => ProviderName;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken) {
            return Task.FromResult<HttpResponseInfo>(null);
        }

        private static bool ShouldFetch(MetadataResult<Episode> itemResult, LibraryOptions libraryOptions) {
            var item = itemResult?.Item;
            return item != null &&
                   libraryOptions != null &&
                   item.IsMetadataFetcherEnabled(libraryOptions, ProviderName) &&
                   IntroDbMarkerSource.GetMissingSegments(itemResult) !=
                   IntroDbMarkerSource.MarkerSegments.None;
        }

        private static ItemUpdateType ApplyMarkers(MetadataResult<Episode> itemResult,
            IntroDbService.MarkerLookupResult result) {
            var item = itemResult?.Item;
            if (item == null || result?.Found != true) return ItemUpdateType.None;

            var missingSegments = IntroDbMarkerSource.GetMissingSegments(itemResult);
            var hasIntro = missingSegments.HasFlag(IntroDbMarkerSource.MarkerSegments.Intro) &&
                           result.IntroStartTicks.HasValue &&
                           result.IntroEndTicks.HasValue &&
                           result.IntroEndTicks.Value > result.IntroStartTicks.Value;
            var hasCredits = missingSegments.HasFlag(IntroDbMarkerSource.MarkerSegments.Credits) &&
                             result.CreditsStartTicks.HasValue &&
                             (!item.RunTimeTicks.HasValue || result.CreditsStartTicks.Value < item.RunTimeTicks.Value);
            if (!hasIntro && !hasCredits) return ItemUpdateType.None;

            var chapters = Plugin.IntroSkipChapterApi.GetChapters(item) ?? new List<ChapterInfo>();
            var updatedMarkerTypes = new List<MarkerType>();
            if (hasIntro) {
                updatedMarkerTypes.Add(MarkerType.IntroStart);
                updatedMarkerTypes.Add(MarkerType.IntroEnd);
            }

            if (hasCredits) updatedMarkerTypes.Add(MarkerType.CreditsStart);
            chapters.RemoveAll(chapter => chapter != null && updatedMarkerTypes.Contains(chapter.MarkerType));

            if (hasIntro) {
                chapters.Add(new ChapterInfo {
                    Name = MarkerType.IntroStart + IntroDbMarkerSource.MarkerSuffix,
                    MarkerType = MarkerType.IntroStart,
                    StartPositionTicks = result.IntroStartTicks.Value
                });
                chapters.Add(new ChapterInfo {
                    Name = MarkerType.IntroEnd + IntroDbMarkerSource.MarkerSuffix,
                    MarkerType = MarkerType.IntroEnd,
                    StartPositionTicks = result.IntroEndTicks.Value
                });
            }

            if (hasCredits) {
                chapters.Add(new ChapterInfo {
                    Name = MarkerType.CreditsStart + IntroDbMarkerSource.MarkerSuffix,
                    MarkerType = MarkerType.CreditsStart,
                    StartPositionTicks = result.CreditsStartTicks.Value
                });
            }

            IntroMarkerProtect.SaveChapters(
                Plugin.Instance.ItemRepository,
                item,
                chapters,
                updatedMarkerTypes,
                filterPlainChapters: false);
            var filledSegments =
                (hasIntro ? IntroDbMarkerSource.MarkerSegments.Intro : IntroDbMarkerSource.MarkerSegments.None) |
                (hasCredits ? IntroDbMarkerSource.MarkerSegments.Credits : IntroDbMarkerSource.MarkerSegments.None);
            IntroDbMarkerSource.MarkSegmentsFilled(itemResult, filledSegments);
            Plugin.Instance.Logger.Info("IntroDB 标记写入成功: {0}", IntroDbService.FormatItemForLog(item));
            return ItemUpdateType.MetadataImport;
        }
    }
}

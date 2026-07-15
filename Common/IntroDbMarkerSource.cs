using System;
using System.Runtime.CompilerServices;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaInfoKeeper.Common {
    internal static class IntroDbMarkerSource {
        [Flags]
        public enum MarkerSegments {
            None = 0,
            Intro = 1,
            Credits = 2,
            All = Intro | Credits
        }

        public const string MarkerSuffix = "#MIKDB";

        private static readonly ConditionalWeakTable<BaseMetadataResult, RefreshMarkerState> RefreshMarkerStates =
            new();

        public static bool IsProviderMarker(ChapterInfo chapter) {
            return chapter?.Name?.EndsWith(MarkerSuffix, StringComparison.OrdinalIgnoreCase) == true;
        }

        public static MarkerSegments GetMissingSegments(BaseMetadataResult refreshResult) {
            if (refreshResult == null || !RefreshMarkerStates.TryGetValue(refreshResult, out var state))
                return MarkerSegments.All;

            lock (state) return MarkerSegments.All & ~state.FilledSegments;
        }

        public static void MarkSegmentsFilled(BaseMetadataResult refreshResult, MarkerSegments segments) {
            if (refreshResult == null || segments == MarkerSegments.None) return;

            var state = RefreshMarkerStates.GetOrCreateValue(refreshResult);
            lock (state) state.FilledSegments |= segments;
        }

        private sealed class RefreshMarkerState {
            public MarkerSegments FilledSegments { get; set; }
        }
    }
}

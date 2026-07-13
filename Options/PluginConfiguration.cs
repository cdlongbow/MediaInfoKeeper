using System.ComponentModel;
using Emby.Web.GenericEdit;

namespace MediaInfoKeeper.Options {
    public class PluginConfiguration : EditableOptionsBase {
        public override string EditorTitle => "MediaInfoKeeper";

        public override string EditorDescription => string.Empty;

        // Main page
        [DisplayName("MediaInfo Keeper")] public MainPageOptions MainPage { get; set; } = new();

        // Tab pages (order follows MainPageController)
        [DisplayName("MediaInfo")] public MediaInfoOptions MediaInfo { get; set; } = new();

        [DisplayName("IntroSkip")] public IntroSkipOptions IntroSkip { get; set; } = new();

        [DisplayName("Enhance")] public EnhanceOptions Enhance { get; set; } = new();

        [DisplayName("MetaData")] public MetaDataOptions MetaData { get; set; } = new();

        [DisplayName("Network")] public NetWorkOptions NetWork { get; set; }

        [DisplayName("GitHub")] public GitHubOptions GitHub { get; set; } = new();

#if DEBUG
        [DisplayName("Debug")]
        public DebugOptions Debug { get; set; } = new DebugOptions();
#endif

        public NetWorkOptions GetNetWorkOptions() {
            var options = NetWork ?? new NetWorkOptions();
            NetWork ??= options;
            return options;
        }

        public MainPageOptions.UpdatePluginTaskEditorOptions GetEffectiveUpdatePluginOptions() {
            MainPage ??= new MainPageOptions();
            MainPage.EnsureScheduledTaskEditors();
            var updatePlugin = MainPage.ScheduledTasksEditor.UpdatePlugin;
            updatePlugin.Initialize();
            return updatePlugin;
        }
    }
}

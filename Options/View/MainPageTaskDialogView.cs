using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Emby.Web.GenericEdit;
using MediaBrowser.Model.GenericEdit;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaBrowser.Model.Plugins.UI.Views.Enums;
using MediaBrowser.Model.Serialization;
using MediaInfoKeeper.Options.UIBaseClasses.Views;

namespace MediaInfoKeeper.Options.View {
    internal abstract class MainPageTaskDialogView<TOptions> : PluginViewBase, IPluginDialogView, IPluginViewWithOptions
        where TOptions : EditableOptionsBase, IEditableObject, new() {
        private readonly string caption;

        protected MainPageTaskDialogView(string pluginId, TOptions options)
            : this(pluginId, options, string.Empty) {
        }

        protected MainPageTaskDialogView(string pluginId, TOptions options, string caption)
            : base(pluginId) {
            ContentData = options;
            this.caption = caption ?? string.Empty;
        }

        public TOptions Options => ContentData as TOptions;

        public bool AllowCancel { get; set; } = true;

        public bool AllowOk { get; set; } = true;

        public bool ShowDialogFullScreen => false;

        public override string Caption => caption;

        public override string SubCaption => string.Empty;

        public virtual Task OnCancelCommand() {
            return Task.CompletedTask;
        }

        public virtual Task OnOkCommand(string providerId, string commandId, string data) {
            ApplyPostedData(data);
            return Task.CompletedTask;
        }

        public PluginViewOptions ViewOptions { get; } = new() {
            DialogSize = DialogSize.MediumTall,
            OKButtonCaption = "保存"
        };

        protected void ApplyPostedData(string data) {
            if (string.IsNullOrWhiteSpace(data) || Options == null) return;

            var jsonSerializer = Plugin.Instance?.AppHost?.Resolve<IJsonSerializer>();
            if (jsonSerializer == null) return;

            try {
                var editable = new TOptions();
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
                var deserialized =
                    editable.DeserializeFromJsonStream(stream,
                        jsonSerializer) as TOptions;
                if (deserialized == null) return;

                CopyWritableProperties(deserialized, Options);
            }
            catch {
            }
        }

        private static void CopyWritableProperties(TOptions source, TOptions target) {
            var properties = typeof(TOptions)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0);

            foreach (var property in properties) property.SetValue(target, property.GetValue(source));
        }
    }
}

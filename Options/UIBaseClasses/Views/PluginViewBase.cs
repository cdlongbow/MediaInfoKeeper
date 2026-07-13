using System;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.GenericEdit;
using MediaBrowser.Model.Plugins.UI.Views;

namespace MediaInfoKeeper.Options.UIBaseClasses.Views {
    internal abstract class PluginViewBase : IPluginUIView {
        protected PluginViewBase(string pluginId) {
            PluginId = pluginId;
        }

        protected virtual IEditableObject ContentDataCore { get; set; }

        public event EventHandler<GenericEventArgs<IPluginUIView>> UIViewInfoChanged;

        public virtual string Caption => ContentData.EditorTitle;

        public virtual string SubCaption => ContentData.EditorDescription;

        public string PluginId { get; protected set; }

        public IEditableObject ContentData {
            get => ContentDataCore;
            set => ContentDataCore = value;
        }

        public UserDto User { get; set; }

        public string RedirectViewUrl { get; set; }

        public virtual bool IsCommandAllowed(string commandKey) {
            return true;
        }

        public virtual Task<IPluginUIView> RunCommand(string itemId, string commandId, string data) {
            return Task.FromResult<IPluginUIView>(null);
        }

        public virtual Task Cancel() {
            return Task.CompletedTask;
        }

        public virtual void OnDialogResult(IPluginUIView dialogView, bool completedOk, object data) {
        }

        protected virtual void RaiseUIViewInfoChanged() {
            UIViewInfoChanged?.Invoke(this, new GenericEventArgs<IPluginUIView>(this));
        }

        protected virtual void RaiseUIViewInfoChanged(GenericEventArgs<IPluginUIView> e) {
            UIViewInfoChanged?.Invoke(this, e);
        }
    }
}

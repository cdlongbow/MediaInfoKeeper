using System;
using Emby.Web.GenericEdit;

namespace MediaInfoKeeper.Options.UIBaseClasses.Store {
    internal class FileSavedEventArgs : EventArgs {
        public FileSavedEventArgs(EditableOptionsBase options) {
            Options = options;
        }

        public EditableOptionsBase Options { get; }
    }
}

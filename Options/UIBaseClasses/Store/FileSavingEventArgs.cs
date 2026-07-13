using System;
using Emby.Web.GenericEdit;

namespace MediaInfoKeeper.Options.UIBaseClasses.Store {
    internal class FileSavingEventArgs : EventArgs {
        public FileSavingEventArgs(EditableOptionsBase options) {
            Options = options;
        }

        public EditableOptionsBase Options { get; }

        public bool Cancel { get; set; }
    }
}

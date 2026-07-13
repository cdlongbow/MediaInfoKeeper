using System;
using Emby.Web.GenericEdit;

namespace MediaInfoKeeper.Options.UIBaseClasses.Store {
    internal class SimpleContentStore<TOptionType>
        where TOptionType : EditableOptionsBase, new() {
        private readonly object lockObj = new();
        private TOptionType options;

        public virtual TOptionType GetOptions() {
            lock (lockObj) {
                if (options == null) options = new TOptionType();

                return options;
            }
        }

        public virtual void SetOptions(TOptionType newOptions) {
            if (newOptions == null) throw new ArgumentNullException(nameof(newOptions));

            lock (lockObj) {
                options = newOptions;
            }
        }
    }
}

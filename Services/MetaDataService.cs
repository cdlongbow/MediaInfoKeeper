using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace MediaInfoKeeper.Services
{
    public class MetaDataService
    {
        private readonly IProviderManager providerManager;

        public MetaDataService(IProviderManager providerManager)
        {
            this.providerManager = providerManager;
        }

        internal async Task RefreshMetaDataAsync(
            BaseItem item,
            MetadataRefreshOptions options,
            CancellationToken cancellationToken)
        {
            if (item == null || options == null)
            {
                return;
            }

            await this.providerManager
                .RefreshFullItem(item, options, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

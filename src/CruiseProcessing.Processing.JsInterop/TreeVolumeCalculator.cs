using CruiseProcessing.Interop;
using CruiseProcessing.Processing.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.JavaScript.NodeApi;

namespace CruiseProcessing.Processing
{
    [JSExport]
    public class TreeVolumeCalculator : IDisposable
    {
        public TreeVolumeCalculator()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging(lb => lb.AddConsole());

            var host = Host = hostBuilder.Build();
        }


        static TreeVolumeCalculator? _instance;
        private bool disposedValue;

        public static TreeVolumeCalculator? Instance
        {
            get => _instance ?? (_instance = new TreeVolumeCalculator());
            //private set => _instance = value;
        }
        protected IHost Host { get; private set; }

        protected void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IVolumeLibrary, VolumeLibrary>();
            services.AddSingleton<CalculateTreeValuesNG>();
        }

        public static NgTreeVolume CalculateTreeVolume(
            NgCruiseInfo cruiseInfo, NgTreeInfo tree, NgUtilizationInfo utilizationValues, IReadOnlyCollection<NgLogInfo> logs)
        {
            var treeCalculator = Instance.Host.Services.GetRequiredService<CalculateTreeValuesNG>();

            return treeCalculator.CalculateTreeVolume(cruiseInfo, tree, utilizationValues, logs, out var messages);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Host?.Dispose();
                    Host = null;
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TreeVolumeCalculator()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

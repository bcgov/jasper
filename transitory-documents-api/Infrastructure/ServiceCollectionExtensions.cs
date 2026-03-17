using Scv.TdApi.Infrastructure.FileSystem;
using Scv.TdApi.Services;

namespace Scv.TdApi.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedDriveServices(
            this IServiceCollection services)
        {
            services.AddSingleton<ISmbClientFactory, SmbClientFactory>();

            services.AddScoped<ISmbFileSystemClient, SmbFileSystemClient>();

            services.AddScoped<ISharedDriveFileService, SharedDriveFileService>();

            return services;
        }
    }
}
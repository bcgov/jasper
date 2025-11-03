using Scv.TdApi.Infrastructure.FileSystem;
using Scv.TdApi.Infrastructure.Options;
using Scv.TdApi.Services;

namespace Scv.TdApi.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedDriveServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<SharedDriveOptions>(configuration.GetSection("SharedDrive"));

            services.Configure<CorrectionMappingOptions>(configuration.GetSection("CorrectionMappings"));

            services.AddScoped<ISmbFileSystemClient, SmbFileSystemClient>();

            services.AddScoped<ISharedDriveFileService, SharedDriveFileService>();

            return services;
        }
    }
}
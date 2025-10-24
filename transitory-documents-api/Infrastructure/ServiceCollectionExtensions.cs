using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            var fileSystemType = configuration.GetValue<string>("SharedDrive:FileSystemType") ?? "LocalFileSystem";

            switch (fileSystemType)
            {
                case "SmbFileSystem":
                    services.AddScoped<ISmbFileSystemClient, SmbFileSystemClient>();
                    break;
                case "LocalFileSystem":
                default:
                    services.AddScoped<ISmbFileSystemClient, LocalFileSystemClient>();
                    break;
            }

            services.AddScoped<ISharedDriveFileService, SharedDriveFileService>();

            return services;
        }
    }
}
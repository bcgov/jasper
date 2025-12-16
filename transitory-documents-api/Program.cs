using dotenv.net;

namespace Scv.TdApi
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            DotEnv.Load();
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    var env = ctx.HostingEnvironment;
                    // OPTIONAL: make the config chain explicit + log
                    cfg.Sources.Clear();
                    cfg.AddJsonFile("appsettings.tdapi.json", optional: false, reloadOnChange: true)
                       .AddJsonFile($"appsettings.tdapi.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                       .AddEnvironmentVariables();

                    var built = cfg.Build();
                    Console.WriteLine($"Environment: {env.EnvironmentName}");
                    Console.WriteLine($"SharedDrive:BasePath = {built["SharedDrive:BasePath"] ?? "<null>"}");
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
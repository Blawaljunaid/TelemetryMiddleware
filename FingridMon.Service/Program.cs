using FingridMon.Service;
using FingridMon.Settings;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Processor>();
        IConfiguration configuration = hostContext.Configuration;

        WorkerOptions options = configuration.GetSection("Settings").Get<WorkerOptions>();

        services.AddSingleton(options);

        services.AddHostedService<Processor>();
    })
    .Build();

await host.RunAsync();

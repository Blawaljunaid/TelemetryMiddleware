using FingridMon.Service;
using FingridMon.Settings;
using Serilog;
using System.Configuration;

IHost host = Host.CreateDefaultBuilder(args)
    //.UseSerilog(configureLogger: (context, configuration) =>
    //{
    //    configuration.Enrich.FromLogContext()
    //    .Enrich.WithMachineName()
    //    .WriteTo.Console()
    //    .WriteTo.Elasticsearch(
    //        new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(context.Configuration["ElasticsearchConfiguration:Uri"]))
    //        {
    //            IndexFormat = $"{context.Configuration["ApplicationName"]}-logs-gabetest-{DateTime.UtcNow:yyyy-MM-dd}",
    //            AutoRegisterTemplate = true,
    //            NumberOfShards = 2,
    //            NumberOfReplicas = 1,

    //        })
    //    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
    //    .ReadFrom.Configuration(context.Configuration);
    //})
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

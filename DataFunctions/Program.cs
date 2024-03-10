using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services;

namespace DataFunctions
{
public class Program
{
    public static void Main()
    {
        var logFac = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = logFac.CreateLogger<Program>();
        logger.LogInformation("Starting the function app. Attempting to configure services.");
        var host = new HostBuilder()
         .ConfigureAppConfiguration((context, config) =>
            {
                //TODO: create a variable for different environments
                //config.AddJsonFile("local.settings.json", optional: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices(ConfigureServices)
            .Build();
        logger.LogInformation("Services Configured. Starting the host..");
        host.Run();
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddLogging();
        services.AddHttpClient();
        services.AddTransient<TableService>(sp =>
        {
            //System.Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var tableServiceConnectionString = context.Configuration.GetWebJobsConnectionString("AzureWebJobsStorage");
            //var tableServiceConnectionString = context.Configuration.GetConnectionString("AzureWebJobsStorage");
            //if (string.IsNullOrEmpty(tableServiceConnectionString)) throw new Exception();
            return new TableService(tableServiceConnectionString, sp.GetService<ILogger<TableService>>(),sp.GetService<HttpClient>());
        });
        services.AddTransient<RestApiService>(sp =>
        {
            return new RestApiService(sp.GetService<ILogger<RestApiService>>(),sp.GetService<HttpClient>());
        });

    }
}
}
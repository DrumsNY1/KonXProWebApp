using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using KonXProWebApp.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient<SocrataClient>(client =>
        {
            client.BaseAddress = new Uri(
                context.Configuration["SocrataBaseUrl"] 
                ?? "https://data.cityofnewyork.us/resource/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var appToken = context.Configuration["SocrataAppToken"];
            if (!string.IsNullOrEmpty(appToken))
            {
                client.DefaultRequestHeaders.Add("X-App-Token", appToken);
            }
        });

        services.AddSingleton<IngestionService>();
        services.AddSingleton<EmailService>();
        services.AddSingleton<SmsService>();
    })
    .Build();

await host.RunAsync();

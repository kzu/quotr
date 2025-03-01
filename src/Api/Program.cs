using System.Net.Http;
using Devlooped;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quote;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication()
    .Configuration.AddUserSecrets<Program>();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Services.AddHttpClient();
#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddHybridCache();

builder.Services.AddDistributedAzureTableStorageCache(options =>
{
    options.ConnectionString = builder.Configuration["AzureWebJobsStorage"];
    options.PartitionKey = "quotes";
    options.TableName = "cache";
});

builder.Services.AddSingleton(sp => MarketStackService.Create(
    sp.GetRequiredService<IHttpClientFactory>(),
    sp.GetRequiredService<IConfiguration>()));

builder.Services.AddSingleton(sp => CloudStorageAccount.Parse(
    sp.GetRequiredService<IConfiguration>()["AzureWebJobsStorage"]));

builder.Build().Run();

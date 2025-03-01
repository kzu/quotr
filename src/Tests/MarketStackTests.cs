using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Quote;

public class MarketStackTests
{
    static IConfiguration configuration;
    static IServiceProvider services;

    static MarketStackTests()
    {
        configuration = new ConfigurationBuilder()
            .AddUserSecrets<MarketStackTests>()
            .AddEnvironmentVariables()
            .Build();

        services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .BuildServiceProvider();
    }

    [SecretsFact("MarketStack:AvailableKey")]
    public async Task CanGetLatestQuote()
    {
        var service = MarketStackService.Create(services.GetRequiredService<IHttpClientFactory>(), configuration["MarketStack:AvailableKey"]!);

        var quote = await service.GetLatestAsync("TSLA");

        Assert.True(quote > 0);
    }

    [SecretsFact("MarketStack:AvailableKey")]
    public async Task CanGetDatedQuote()
    {
        var service = MarketStackService.Create(services.GetRequiredService<IHttpClientFactory>(), configuration["MarketStack:AvailableKey"]!);

        var quote = await service.GetQuoteAsync("TSLA", new DateOnly(2025, 2, 27));

        Assert.NotNull(quote);
        Assert.True(quote > 0);
    }

    [SecretsFact("MarketStack:AvailableKey")]
    public async Task CanGetWeekendQuote()
    {
        var service = MarketStackService.Create(services.GetRequiredService<IHttpClientFactory>(), configuration["MarketStack:AvailableKey"]!);

        var quote = await service.GetQuoteAsync("TSLA", new DateOnly(2025, 2, 22));

        Assert.Null(quote);
    }

    [SecretsFact("MarketStack:LimitKey")]
    public async Task ThrowsIfKeyLimit()
    {
        var service = MarketStackService.Create(services.GetRequiredService<IHttpClientFactory>(), configuration["MarketStack:LimitKey"]!);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(async () => await service.GetQuoteAsync("TSLA", new DateOnly(2025, 2, 22)));

        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, ex.StatusCode);
    }

    [SecretsFact("MarketStack:AvailableKey", "MarketStack:LimitKey")]
    public async Task CyclesFailingAccessKey()
    {
        var service = MarketStackService.Create(services.GetRequiredService<IHttpClientFactory>(), configuration);

        var quote = await service.GetLatestAsync("TSLA");

        Assert.True(quote > 0);
    }
}

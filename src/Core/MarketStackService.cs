using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Quote;

public static class MarketStackService
{
    public static IQuoteService Create(IHttpClientFactory factory, IConfiguration configuration)
    {
        var keys = configuration["MarketStack:Keys"] ?? throw new InvalidOperationException("MarketStack:Keys is missing");
        return new CompositeQuoteService([.. keys.Split(';', '|').Select(key => new AccessKeyQuoteService(factory, key))]);
    }

    public static IQuoteService Create(IHttpClientFactory factory, string accessKey)
        => new AccessKeyQuoteService(factory, accessKey);

    class CompositeQuoteService(IList<IQuoteService> services) : IQuoteService
    {
        static readonly Random random = new();

        public async ValueTask<double> GetLatestAsync(string symbol)
        {
            while (true)
            {
                var service = services[random.Next(services.Count)];
                try
                {
                    return await service.GetLatestAsync(symbol);
                }
                catch (Exception)
                {
                    services.Remove(service);
                    if (services.Count == 0)
                        throw;
                }
            }
        }

        public async ValueTask<double?> GetQuoteAsync(string symbol, DateOnly date)
        {
            while (true)
            {
                var service = services[random.Next(services.Count)];
                try
                {
                    return await service.GetQuoteAsync(symbol, date);
                }
                catch (Exception)
                {
                    services.Remove(service);
                    if (services.Count == 0)
                        throw;
                }
            }
        }
    }

    class AccessKeyQuoteService(IHttpClientFactory factory, string accessKey) : IQuoteService
    {
        public async ValueTask<double> GetLatestAsync(string symbol) => await GetTickerAsync(symbol, "latest") ??
            throw new InvalidOperationException("Should never fail.");

        public ValueTask<double?> GetQuoteAsync(string symbol, DateOnly date) => GetTickerAsync(symbol, date.ToString("yyyy-MM-dd"));

        async ValueTask<double?> GetTickerAsync(string symbol, string filter)
        {
            using var client = factory.CreateClient();
            var url = $"https://api.marketstack.com/v2/tickers/{symbol.Replace('.', '-')}/eod/{filter}?access_key={accessKey}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            if (filter == "latest")
            {
                if (await response.Content.ReadFromJsonAsync<Ticker>() is not { } latest)
                    return null;

                return latest.Close;
            }

            // We can get an empty response if request came for a non-trading day.
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(json) || json == "[]")
                return null;

            if (await response.Content.ReadFromJsonAsync<Ticker>() is not { } ticker)
                return null;

            return ticker.Close;
        }

        record Ticker(double Close, [property: JsonConverter(typeof(DateOnlyJsonConverter))] DateOnly Date);
    }
}
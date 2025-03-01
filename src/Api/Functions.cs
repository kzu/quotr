using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Quote;

#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class Functions(IConfiguration configuration, IQuoteService quotes, HybridCache cache, ILogger<Functions> logger)
{
    [Function("quote")]
    public async Task<HttpResponseData> Quote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "quote/{symbol:alpha}/{date::regex(^\\d{{4}}-\\d{{1,2}}-\\d{{1,2}}$)?}")] HttpRequestData request,
        string symbol, string? date)
    {
        if (IsForbidden(request))
            return request.CreateResponse(HttpStatusCode.Forbidden);

        try
        {
            var cacheDate = date == null ? DateOnly.FromDateTime(DateTime.Now) : DateOnly.Parse(date);
            var cacheKey = $"{symbol}-{cacheDate:yyyy-MM-dd}";

            var quote = await cache.GetOrCreateAsync(
                cacheKey,
                async cancellation => date == null
                    ? await quotes.GetLatestAsync(symbol)
                    : await quotes.GetQuoteAsync(symbol, DateOnly.Parse(date)));

            if (quote == null)
            {
                logger.LogInformation($"Couldn't get quote for symbol {symbol}");
                return request.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(quote.ToString() ?? "");

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting quote for {symbol}", symbol);
            return request.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    bool IsForbidden(HttpRequestData request)
    {
        if (configuration["AccessKey"] is { Length: > 0 } key)
        {
            if (request.Headers.TryGetValues("x-access_key", out var values) &&
                values.ToString() == key)
            {
                return false;
            }

            if (request.Query["access_key"] is { } query &&
                query == key)
            {
                return false;
            }

            // the key is configured and was not provided
            return true;
        }

        return false;
    }
}
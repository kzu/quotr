using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Quote;

public class Functions(IQuoteService quotes, ILogger<Functions> logger)
{
    [Function("quote")]
    public async Task<IActionResult> Quote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "quote/{symbol:alpha}/{date::regex(^\\d{{4}}-\\d{{1,2}}-\\d{{1,2}}$)?}")] HttpRequest request,
        string symbol, string? date)
    {
        try
        {
            var quote = date == null
                ? await quotes.GetLatestAsync(symbol)
                : await quotes.GetQuoteAsync(symbol, DateOnly.Parse(date));

            if (quote == null)
            {
                logger.LogInformation($"Couldn't get quote for symbol {symbol}");
                return new NotFoundResult();
            }

            return new OkObjectResult(quote);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting quote for {symbol}", symbol);
            return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
        }
    }
}

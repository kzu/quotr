using System;
using System.Threading.Tasks;

namespace Quote;

/// <summary>
/// A service for retrieving quotes.
/// </summary>
public interface IQuoteService
{
    /// <summary>
    /// Gets the live quote (or latest known, if it's a non-trading day) 
    /// for the given symbol.
    /// </summary>
    ValueTask<double> GetLatestAsync(string symbol);
    /// <summary>
    /// Gets the quote for the given symbol on the given date at closing time.
    /// </summary>
    ValueTask<double?> GetQuoteAsync(string symbol, DateOnly date);
}

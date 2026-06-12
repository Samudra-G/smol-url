namespace UrlShortener.Core.Interfaces;

/// <summary>
/// Specifies the evaluation state of a cache query key.
/// </summary>
public enum CacheStatus
{
    /// <summary>The data requested was not present in the distributed memory layer.</summary>
    Miss,
    /// <summary>The data requested was successfully located and fetched.</summary>
    Hit,
    /// <summary>The key was explicitly verified and cached as non-existent or inactive to guard databases against scraping.</summary>
    NegativeHit
}

/// <summary>
/// Represents a value-typed wrapper containing the lifecycle result and payload of a cache operation.
/// Structured as an immutable, stack-allocated record to optimize heap allocations on extreme request rates.
/// </summary>
/// <param name="Status">The evaluation outcome of the cache read cycle.</param>
/// <param name="LongUrl">The cached absolute URL payload, if a successful <see cref="CacheStatus.Hit"/> occurred.</param>
public readonly record struct CacheLookupResult(CacheStatus Status, string? LongUrl)
{
    /// <summary>Represents a pre-allocated cache miss state indicator.</summary>
    public static readonly CacheLookupResult Miss = new(CacheStatus.Miss, null);
    
    /// <summary>Represents a pre-allocated tombstone indicator confirming the record does not exist.</summary>
    public static readonly CacheLookupResult NotFound = new(CacheStatus.NegativeHit, null);

    /// <summary>
    /// Factory pattern helper to generate a valid, found data state payload wrapper.
    /// </summary>
    /// <param name="longUrl">The localized target destination URL address.</param>
    /// <returns>An initialized instance of <see cref="CacheLookupResult"/> containing data indicators.</returns>
    public static CacheLookupResult Found(string longUrl) => new(CacheStatus.Hit, longUrl);
}

/// <summary>
/// Defines a low-latency, distributed key-value abstraction mechanism to isolate the relational database layer.
/// </summary>
public interface IUrlCache
{
    /// <summary>
    /// Queries the distributed store asynchronously to inspect data presence for a target unique internal numeric sequence code.
    /// </summary>
    /// <param name="id">The unique numerical index representing the shortened sequence mapping.</param>
    /// <returns>A lightweight value structure summarizing availability indicators and data properties.</returns>
    Task<CacheLookupResult> GetAsync(long id);

    /// <summary>
    /// Caches an active destination destination string resource payload mapped directly against its numerical identity sequence.
    /// </summary>
    /// <param name="id">The unique numerical index representing the shortened sequence mapping.</param>
    /// <param name="longUrl">The original uncompressed resource target destination URL.</param>
    Task SetAsync(long id, string longUrl);

    /// <summary>
    /// Persists an authoritative tombstone record to memory to confirm a target key represents a missing or deleted resource.
    /// </summary>
    /// <param name="id">The unique numerical index representing the shortened sequence mapping.</param>
    Task SetNotFoundAsync(long id);
}
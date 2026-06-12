using UrlShortener.Core.Entities;

namespace UrlShortener.Core.Interfaces;

/// <summary>
/// Defines the authoritative persistence contract for managing <see cref="ShortUrl"/> domain records.
/// Handles transactional write mutations and low-latency index lookups against the primary relational store.
/// </summary>
public interface IUrlRepository
{
    /// <summary>
    /// Persists a new short URL configuration entity into the database.
    /// </summary>
    /// <param name="shortUrl">The un-persisted domain entity containing the source URL configuration and tenant metadata.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for operation cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the persisted entity populated with its generated 64-bit primary sequence key.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the execution context cancellation token is triggered before completion.</exception>
    Task<ShortUrl> CreateAsync(ShortUrl shortUrl, CancellationToken ct = default);

    /// <summary>
    /// Fetches an active or inactive short URL configuration entity by its unique internal numeric identity sequence.
    /// This lookup hits indexed primary boundaries for maximum extraction efficiency.
    /// </summary>
    /// <param name="id">The unique internal 64-bit numerical tracking identity representing the short record.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for operation cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the populated <see cref="ShortUrl"/> entity if found; otherwise, null.</returns>
    Task<ShortUrl?> GetByIdAsync(long id, CancellationToken ct = default);
}
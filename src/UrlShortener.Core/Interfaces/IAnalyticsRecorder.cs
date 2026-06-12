namespace UrlShortener.Core.Interfaces;

/// <summary>
/// Defines a decoupled telemetry ingestion mechanic for recording high-throughput routing redirection performance.
/// </summary>
public interface IAnalyticsRecorder
{
    /// <summary>
    /// Ingests click telemetry tracking metrics asynchronously for a successfully resolved short token id.
    /// Implementation should ideally leverage non-blocking, asynchronous pipelines (e.g., streaming buffers, message brokers, or event busses)
    /// to prevent telemetry processing from introducing performance degradation on the HTTP redirect hot path.
    /// </summary>
    /// <param name="urlId">The underlying 64-bit numeric identity sequence code of the resolved short URL address.</param>
    /// <returns>A task representing the asynchronous execution context acknowledgment.</returns>
    Task RecordClickAsync(long urlId);
}
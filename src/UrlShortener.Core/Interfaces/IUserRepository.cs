using UrlShortener.Core.Entities;

namespace UrlShortener.Core.Interfaces;

/// <summary>
/// Defines the data access contract for user account management, authentication, and session tokens.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching <see cref="User"/>, or <see langword="null"/> if not found.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching <see cref="User"/>, or <see langword="null"/> if not found.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user linked to a specific external OAuth/OIDC provider configuration.
    /// </summary>
    /// <param name="provider">The name of the external identity provider (e.g., Google, GitHub).</param>
    /// <param name="providerKey">The unique provider-issued identifier for the user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching <see cref="User"/>, or <see langword="null"/> if not found.</returns>
    Task<User?> GetByExternalLoginAsync(string provider, string providerKey, CancellationToken ct = default);

    /// <summary>
    /// Persists a new user record in the storage system.
    /// </summary>
    /// <param name="user">The user entity to create.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The newly created <see cref="User"/> entity with its generated identifiers.</returns>
    Task<User> CreateAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Links an external login provider profile to an existing user account.
    /// </summary>
    /// <param name="login">The external login configuration details.</param>
    /// <param name="ct">The cancellation token.</param>
    Task AddExternalLoginAsync(UserExternalLogin login, CancellationToken ct = default);

    /// <summary>
    /// Generates and persists a new API refresh token.
    /// </summary>
    /// <param name="token">The refresh token details to save.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The persisted <see cref="RefreshToken"/> record.</returns>
    Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a refresh token by its hashed value if it has not expired or been revoked.
    /// </summary>
    /// <param name="tokenHash">The secure cryptographic hash of the raw refresh token string.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The valid <see cref="RefreshToken"/>, or <see langword="null"/> if invalid, expired, or revoked.</returns>
    Task<RefreshToken?> GetActiveRefreshTokenAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Invalidates an active refresh token to end a user session.
    /// </summary>
    /// <param name="token">The refresh token record to revoke.</param>
    /// <param name="ct">The cancellation token.</param>
    Task RevokeRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
}
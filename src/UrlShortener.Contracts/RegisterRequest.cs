using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Contracts;

public record RegisterRequest(
    [Required][EmailAddress]string Email, 
    [Required][MinLength(8)]string Password, 
    string? DisplayName
);
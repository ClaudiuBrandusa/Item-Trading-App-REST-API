namespace Application.Results.Identity;

public record AuthenticationResult(
    string Token,
    string RefreshToken,
    DateTime ExpirationDateTime
);

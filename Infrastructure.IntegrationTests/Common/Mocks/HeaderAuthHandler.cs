using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Infrastructure.IntegrationTests.Common.Mocks;

public sealed class HeaderAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "HeaderTestAuth";
    public const string HeaderUserId = "x-user-id";
    public const string HeaderUserName = "x-test-user";

    public HeaderAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock systemClock) : base(options, logger, encoder, systemClock)
    { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers[HeaderUserId].ToString();
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(AuthenticateResult.NoResult()); // or Fail(...)

        var userName = Request.Headers[HeaderUserName].ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
        };

        if (!string.IsNullOrWhiteSpace(userName))
            claims.Add(new Claim(ClaimTypes.Name, userName));

        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
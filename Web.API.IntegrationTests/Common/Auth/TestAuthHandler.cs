using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Web.API.IntegrationTests.Common.Auth;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthSchemeName = "TestScheme";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers[Constants.UserIdRequestHeaderName].ToString();
        var userName = Request.Headers[Constants.UserNameRequestHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(userName)) userName = Constants.DefaultTestUserName;

        var role = Request.Headers[Constants.UserRoleRequestHeaderName].ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("id", userId),
            new(ClaimTypes.Name, userName),
        };
        if (!string.IsNullOrWhiteSpace(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthSchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, AuthSchemeName)));
    }
}

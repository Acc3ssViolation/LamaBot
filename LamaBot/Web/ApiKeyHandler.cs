using LamaBot.Servers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace LamaBot.Web
{
    public class ApiKeyHandler : AuthenticationHandler<ApiKeyOptions>
    {
        private readonly IServerSettings _settings;

        public ApiKeyHandler(IServerSettings settings, IOptionsMonitor<ApiKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {
            _settings = settings;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!(Request.RouteValues.TryGetValue("guildId", out var guildIdObj) &&
                guildIdObj is string guildIdString && 
                ulong.TryParse( guildIdString, out var guildId)))
                return AuthenticateResult.Fail("No guildId in route");

            string? key = null;
            if (Request.Query.TryGetValue("key", out var queryKeys))
            {
                key = queryKeys.FirstOrDefault();
            }
            else if (Request.Headers.TryGetValue("X-API-Key", out var apiKeys))
            {
                key = apiKeys.FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(key))
                return AuthenticateResult.Fail("No key provided");

            var expectedKey = await _settings.GetSettingAsync(guildId, Quotes.QuoteSettings.QuoteApiKey).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(expectedKey))
                return AuthenticateResult.Fail("No key stored for guild");

            if (!expectedKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                return AuthenticateResult.Fail("Key invalid");

            var claims = new List<Claim>
            {
                new(ClaimTypes.Role, "QuoteReader"),
            };
            var identity = new ClaimsIdentity(claims, "ApiKey");
            var principal = new ClaimsPrincipal(identity);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
    }
}

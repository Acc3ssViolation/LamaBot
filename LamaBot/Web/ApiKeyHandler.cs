using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace LamaBot.Web
{
    public class ApiKeyHandler : AuthenticationHandler<ApiKeyOptions>
    {
        private readonly IApiKeyRepository _apiKeyRepository;

        public ApiKeyHandler(IApiKeyRepository apiKeyRepository, IOptionsMonitor<ApiKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {
            _apiKeyRepository = apiKeyRepository;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!(Request.RouteValues.TryGetValue("guildId", out var guildIdObj) &&
                guildIdObj is string guildIdString &&
                ulong.TryParse(guildIdString, out var guildId)))
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

            var apiKeyInfo = await _apiKeyRepository.GetApiKeyInfoAsync(guildId, key, Context.RequestAborted).ConfigureAwait(false);
            if (apiKeyInfo == null)
                return AuthenticateResult.Fail("Key invalid");

            var claims = apiKeyInfo.Roles.Select(r => new Claim(ClaimTypes.Role, r));
            var identity = new ClaimsIdentity(claims, "ApiKey");
            var principal = new ClaimsPrincipal(identity);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Security.Claims;

namespace LamaBot.Web
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireRoleAttribute(string role) : Attribute, IAuthorizationFilter
    {
        public string Role { get; } = role ?? throw new ArgumentNullException(nameof(role));

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var hasRole = user.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == Role);
            if (!hasRole)
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }
    }
}

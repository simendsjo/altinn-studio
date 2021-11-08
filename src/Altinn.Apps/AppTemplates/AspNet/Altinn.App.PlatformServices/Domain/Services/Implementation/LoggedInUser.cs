using System;
using System.Security.Claims;

using Altinn.App.Domain.Services.Interface;
using Altinn.App.Services.Configuration;

using AltinnCore.Authentication.Constants;
using AltinnCore.Authentication.Utils;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Altinn.App.Domain.Services.Implementation
{
    /// <summary>
    /// Service for retrieving information about the logged in user
    /// </summary>
    public class LoggedInUser : ILoggedInUser
    {
        private readonly HttpContext _httpContext;
        private readonly string _cookieName;
        private readonly ClaimsPrincipal _user;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggedInUser"/> class.
        /// </summary>
        public LoggedInUser(IHttpContextAccessor httpContextAccessor, IOptionsMonitor<AppSettings> settings)
        {
            _httpContext = httpContextAccessor.HttpContext;
            _cookieName = settings.CurrentValue.RuntimeCookieName;
            _user = _httpContext.User;
        }

        /// <inheritdoc/>
        public string GetToken()
        {
            return JwtTokenUtil.GetTokenFromContext(_httpContext, _cookieName);
        }

        /// <inheritdoc/>
        public string GetOrg()
        {
            if (_user.HasClaim(c => c.Type == AltinnCoreClaimTypes.Org))
            {
                Claim orgClaim = _user.FindFirst(c => c.Type == AltinnCoreClaimTypes.Org);
                if (orgClaim != null)
                {
                    return orgClaim.Value;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public int? GetOrgNumber()
        {
            if (_user.HasClaim(c => c.Type == AltinnCoreClaimTypes.OrgNumber))
            {
                Claim orgClaim = _user.FindFirst(c => c.Type == AltinnCoreClaimTypes.OrgNumber);
                if (orgClaim != null && int.TryParse(orgClaim.Value, out int orgNumber))
                {
                    return orgNumber;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public int? GetUserIdAsInt()
        {
            if (_user.HasClaim(c => c.Type == AltinnCoreClaimTypes.UserId))
            {
                Claim userIdClaim = _user.FindFirst(c => c.Type == AltinnCoreClaimTypes.UserId);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public ClaimsPrincipal GetUserPrincipal()
        {
            return _user;
        }
    }
}

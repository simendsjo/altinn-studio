using System.Security.Claims;

namespace Altinn.App.Domain.Services.Interface
{
    /// <summary>
    /// Interface for handling the logged in user 
    /// </summary>
    public interface ILoggedInUser
    {
        /// <summary>
        /// Gets the authentication token for the logged in user
        /// </summary>
        public string GetToken();

        /// <summary>
        /// Returns the org claim for the logged in user or null if the claim does not exist.
        /// </summary>
        public string GetOrg();

        /// <summary>
        /// Returns the organisation number of an org user or null if claim does not exist.
        /// </summary>
        public int? GetOrgNumber();

        /// <summary>
        /// Return the userId as an int or null if UserId claim is not set.
        /// </summary>
        public int? GetUserIdAsInt();

        /// <summary>
        /// Gets the claims principal.
        /// </summary>
        public ClaimsPrincipal GetUserPrincipal();
    }
}

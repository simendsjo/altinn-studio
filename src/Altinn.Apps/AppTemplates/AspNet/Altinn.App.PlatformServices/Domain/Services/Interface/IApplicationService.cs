using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Domain.Services.Interface
{
    /// <summary>
    /// Interface for handling application metadata related operations
    /// </summary>
    public interface IApplicationService
    {
        /// <summary>
        /// Gets the application metadata
        /// </summary>
        /// <returns></returns>
        public Application GetApplication();
    }
}

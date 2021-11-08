using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Domain.Models
{
    /// <summary>
    /// Class describing an InstanceService response
    /// </summary>
    public class InstanceResponse
    {
        /// <summary>
        /// The instance object
        /// </summary>
        public Instance Instance { get; set; }

        /// <summary>
        /// The service result
        /// </summary>
        public ServiceResult Result { get; set; }
    }
}

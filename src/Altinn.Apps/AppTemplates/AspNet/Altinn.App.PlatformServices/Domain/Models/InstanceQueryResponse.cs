using System.Collections.Generic;

using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Domain.Models
{
    /// <summary>
    /// Class representing an instance query response
    /// </summary>
    public class InstanceQueryResponse
    {
        /// <summary>
        /// A list of instance objects
        /// </summary>
        public List<Instance> Instances { get; set; }

        /// <summary>
        /// The service result
        /// </summary>
        public ServiceResult Result { get; set; }
    }
}

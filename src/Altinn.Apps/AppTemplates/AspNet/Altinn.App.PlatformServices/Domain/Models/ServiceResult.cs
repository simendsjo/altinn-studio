using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Domain.Models
{
    /// <summary>
    /// A class that describes a service call result
    /// </summary>
    public class ServiceResult
    {
        /// <summary>
        /// Boolean indicating if the action was successful
        /// </summary>
        public bool Successful { get; set; }

        /// <summary>
        /// HttpStatusCode indicating the status
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Error message from service or dependencies
        /// </summary>
        public object ErrorObject { get; set; }
    }
}

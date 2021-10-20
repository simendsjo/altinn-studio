using System;
using System.Collections.Generic;
using Altinn.Platform.Storage.Interface.Models;
using Newtonsoft.Json;

namespace Altinn.App.Api.Models
{
    /// <summary>
    /// Specialized model for isntansiation of instances
    /// </summary>
    public class InstanceInstansiation
    {
        /// <summary>
        /// Gets or sets the instance owner information. 
        /// </summary>
        [JsonProperty(PropertyName = "instanceOwner")]
        public InstanceOwner InstanceOwner { get; set; }

        /// <summary>
        /// Gets or sets the due date to submit the instance to application owner.
        /// </summary>
        [JsonProperty(PropertyName = "dueBefore")]
        public DateTime? DueBefore { get; set; }

        /// <summary>
        /// Gets or sets date and time for when the instance should first become visible for the instance owner.
        /// </summary>
        [JsonProperty(PropertyName = "visibleAfter")]
        public DateTime? VisibleAfter { get; set; }

        /// <summary>
        /// Gets or sets the prefill values for the instance.        
        /// </summary>
        [JsonProperty(PropertyName = "prefill")]
        public Dictionary<string, string> Prefill { get; set; }
    }
}

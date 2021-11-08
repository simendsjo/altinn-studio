using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Domain.Domains
{
    /// <summary>
    /// Domain class for instance
    /// </summary>
    public static class InstanceDomain
    {
        /// <summary>
        /// Sets self links on the instance and its data elements
        /// </summary>
        /// <param name="appBaseUrl">The application base url including schema, host, and base path for app resources.</param>
        /// <param name="instance">The instance</param>
        public static void SetAppSelfLinks(string appBaseUrl, Instance instance)
        {
            int start = appBaseUrl.IndexOf("/instances");
            if (start > 0)
            {
                appBaseUrl = appBaseUrl.Substring(0, start) + "/instances";
            }

            appBaseUrl += $"/{instance.Id}";

            if (!appBaseUrl.EndsWith(instance.Id))
            {
                appBaseUrl += instance.Id;
            }

            instance.SelfLinks ??= new ResourceLinks();
            instance.SelfLinks.Apps = appBaseUrl;

            if (instance.Data != null)
            {
                foreach (DataElement dataElement in instance.Data)
                {
                    dataElement.SelfLinks ??= new ResourceLinks();
                    dataElement.SelfLinks.Apps = $"{appBaseUrl}/data/{dataElement.Id}";
                }
            }
        }
    }
}

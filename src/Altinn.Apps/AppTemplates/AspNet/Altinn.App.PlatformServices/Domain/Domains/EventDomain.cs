using System;

using Altinn.App.Domain.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Domain.Domains
{
    /// <summary>
    /// Domain logic for handling events
    /// </summary>
    public class EventDomain
    {
        /// <summary>
        /// Creates a cloud events
        /// </summary>
        public static CloudEvent CreateCloudEvent(string eventType, string appBaseUrl, Instance instance)
        {
            string alternativeSubject = null;
            if (!string.IsNullOrWhiteSpace(instance.InstanceOwner.OrganisationNumber))
            {
                alternativeSubject = $"/org/{instance.InstanceOwner.OrganisationNumber}";
            }

            if (!string.IsNullOrWhiteSpace(instance.InstanceOwner.PersonNumber))
            {
                alternativeSubject = $"/person/{instance.InstanceOwner.PersonNumber}";
            }

            CloudEvent cloudEvent = new CloudEvent
            {
                Subject = $"/party/{instance.InstanceOwner.PartyId}",
                Type = eventType,
                AlternativeSubject = alternativeSubject,
                Time = DateTime.UtcNow,
                SpecVersion = "1.0",
                Source = new Uri($"{appBaseUrl}/instances/{instance.Id}")
            };

            return cloudEvent;
        }
    }
}
